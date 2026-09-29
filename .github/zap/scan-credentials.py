import json
import os
import re
import secrets
import sys
import urllib.error
import urllib.request

BaseUrl = os.environ["BaseUrl"]

IdentityFile = "scan-identity.json"
ApiKeyRules = "zap-auth.conf"
CookieRules = "zap-auth-cookie.conf"


def call(method, path, token=None, body=None):
    data = None if body is None else json.dumps(body).encode()
    request = urllib.request.Request(BaseUrl + path, data=data, method=method)
    request.add_header("Accept", "application/json")
    if data is not None:
        request.add_header("Content-Type", "application/json")
        request.add_header("Origin", BaseUrl)
    if token:
        request.add_header("Cookie", "authentication-jwt=" + token)
    try:
        with urllib.request.urlopen(request) as response:
            raw = response.read().decode()
            return response.status, raw, response.headers
    except urllib.error.HTTPError as error:
        return error.code, error.read().decode(), error.headers


def decoded(raw):
    try:
        return json.loads(raw) if raw.strip() else {}
    except json.JSONDecodeError:
        return {}


def expect(method, path, token=None, body=None, what=""):
    status, raw, headers = call(method, path, token, body)
    if status != 200:
        raise RuntimeError(f"{what or method + ' ' + path} returned HTTP {status}: {raw[:400]}")
    return decoded(raw), headers


def mask(value):
    if value:
        print("::add-mask::" + value)


def strong_password():
    while True:
        candidate = "Zap!" + secrets.token_hex(8) + "Aa9"
        if not re.search(r"(.)\1{2,}", candidate):
            return candidate


def cookie_from(headers):
    for value in headers.get_all("Set-Cookie") or []:
        found = re.match(r"authentication-jwt=([^;]+)", value)
        if found and found.group(1):
            return found.group(1)
    return None


def jwt_from_jar(path="cookies.txt"):
    with open(path) as handle:
        found = re.search(r"authentication-jwt\s+(\S+)", handle.read())
    if not found:
        raise RuntimeError(f"{path} holds no authentication-jwt cookie")
    return found.group(1)


def write_rules(path, rules):
    with open(path, "w") as handle:
        for index, (description, header, value) in enumerate(rules):
            handle.write(f"replacer.full_list({index}).description={description}\n")
            handle.write(f"replacer.full_list({index}).enabled=true\n")
            handle.write(f"replacer.full_list({index}).matchtype=REQ_HEADER\n")
            handle.write(f"replacer.full_list({index}).matchstr={header}\n")
            handle.write(f"replacer.full_list({index}).regex=false\n")
            handle.write(f"replacer.full_list({index}).replacement={value}\n")
    print(f"Replacer rules written to {path}:")
    for index, (description, header, _) in enumerate(rules):
        print(f"  ({index}) {description}: sets request header {header} to <redacted>")


def sign_in(user_name, password, new_password=None):
    body = {"userName": user_name, "password": password}
    if new_password:
        body["newPassword"] = new_password
        body["repeatNewPassword"] = new_password
    status, raw, headers = call("POST", "/api/Authentication/ByUserNamePassword", body=body)
    if status != 200:
        raise RuntimeError(f"Sign-in as {user_name} returned HTTP {status}: {raw[:400]}")
    jwt = cookie_from(headers)
    if not jwt:
        raise RuntimeError(f"Sign-in as {user_name} issued no authentication-jwt cookie")
    mask(jwt)
    return jwt


def mint_api_key(token, user_registry_id, user_name):
    created, _ = expect("POST", "/api/UserRegistryApiKey", token=token, body={
        "userRegistryId": user_registry_id,
        "name": "ZAP DAST",
        "description": "Ephemeral key minted for the nightly ZAP API scan",
    }, what="Minting the API key")
    api_key = created.get("apiKeyDisplay")
    if not api_key:
        raise RuntimeError("The API key response carried no plaintext key")
    mask(api_key)
    print(f"Minted an API key for {user_name} (UserRegistry id {user_registry_id})")
    return api_key


def find_user(token, user_name):
    users, _ = expect("GET", "/api/UserRegistry", token=token, what="Listing users")
    for user in users:
        if str(user.get("name", "")).casefold() == user_name.casefold():
            return user
    return None


def provision_scan_identity(administrator_token):
    administrator = find_user(administrator_token, "Administrator")
    if administrator is None:
        raise RuntimeError("No Administrator row in /api/UserRegistry")

    user_name = "zap-dast-" + secrets.token_hex(4)
    expect("POST", "/api/UserRegistry", token=administrator_token, body={
        "name": user_name,
        "roleRegistryGuid": administrator["roleRegistryGuid"],
        "email": f"{user_name}@dast.invalid",
        "active": True,
        "wirePasswordHash": False,
    }, what=f"Creating the scan identity {user_name}")

    created = find_user(administrator_token, user_name)
    if created is None:
        raise RuntimeError(f"The scan identity {user_name} was created but is not listed")

    issued, _ = expect("POST", "/api/UserRegistry/SetPassword", token=administrator_token,
                       body={"id": created["id"], "wirePasswordHash": False},
                       what="Issuing the scan identity a temporary password")
    temporary = issued.get("password")
    if not temporary:
        raise RuntimeError("No temporary password was issued for the scan identity")
    mask(temporary)

    password = strong_password()
    mask(password)
    token = sign_in(user_name, temporary, password)

    print(f"Provisioned scan identity {user_name} as UserRegistry id {created['id']}, "
          f"sharing the Administrator role")
    return user_name, password, created["id"], token


def provision():
    administrator_token = jwt_from_jar()
    mask(administrator_token)

    try:
        user_name, password, user_registry_id, token = provision_scan_identity(administrator_token)
    except (RuntimeError, urllib.error.URLError, KeyError) as error:
        print(f"::warning::Could not provision a dedicated scan identity ({error}). Falling back to "
              f"the seeded Administrator, which is UserRegistry id 1 and so is reachable by the "
              f"default id ZAP generates for {{id}} in the administration pass; that pass may "
              f"therefore curtail its own coverage.")
        administrator = find_user(administrator_token, "Administrator")
        if administrator is None:
            raise RuntimeError("No Administrator row in /api/UserRegistry") from error
        user_name, password, user_registry_id, token = None, None, administrator["id"], administrator_token

    api_key = mint_api_key(token, user_registry_id, user_name or "Administrator")
    write_rules(ApiKeyRules, [("jube-api-key", "X-API-KEY", api_key)])

    with open(IdentityFile, "w") as handle:
        json.dump({"userName": user_name, "password": password,
                   "userRegistryId": user_registry_id}, handle)


def cookie():
    output = os.environ.get("GITHUB_OUTPUT")

    def unavailable(reason):
        print(f"::warning::{reason} The cookie pass over the authentication surface will be "
              f"skipped, so the browser-facing CSRF path is not exercised by this run.")
        if output:
            with open(output, "a") as handle:
                print("ready=false", file=handle)

    try:
        with open(IdentityFile) as handle:
            identity = json.load(handle)
    except (OSError, json.JSONDecodeError) as error:
        return unavailable(f"The scan identity could not be read ({error}).")

    if not identity.get("userName") or not identity.get("password"):
        return unavailable("No dedicated scan identity was provisioned, so there is no password "
                           "with which to mint a fresh session cookie.")

    mask(identity["password"])

    try:
        token = sign_in(identity["userName"], identity["password"])
    except (RuntimeError, urllib.error.URLError) as error:
        return unavailable(f"The scan identity could not sign in again ({error}).")

    write_rules(CookieRules, [
        ("jube-session-cookie", "Cookie", "authentication-jwt=" + token),
        ("jube-same-origin", "Origin", BaseUrl),
    ])

    if output:
        with open(output, "a") as handle:
            print("ready=true", file=handle)


if __name__ == "__main__":
    if len(sys.argv) != 2:
        sys.exit("usage: scan-credentials.py provision|cookie")
    if sys.argv[1] == "provision":
        provision()
    elif sys.argv[1] == "cookie":
        cookie()
    else:
        sys.exit("usage: scan-credentials.py provision|cookie")
