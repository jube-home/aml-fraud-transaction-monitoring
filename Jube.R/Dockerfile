# syntax=docker/dockerfile:1.7

ARG R_VERSION=4.5.1

FROM rocker/r-ver:${R_VERSION} AS build

RUN apt-get update && apt-get install -y --no-install-recommends \
      libcurl4-openssl-dev libssl-dev libxml2-dev zlib1g-dev \
      libfreetype6-dev libpng-dev libtiff5-dev libjpeg-dev \
      libfontconfig1-dev libharfbuzz-dev libfribidi-dev \
      unixodbc-dev \
      libsodium-dev libuv1-dev cmake \
      libglpk40 \
    && rm -rf /var/lib/apt/lists/*

ENV R_LIBRARY=/opt/r-library
RUN mkdir -p "${R_LIBRARY}"
ENV USE_BUNDLED_LIBUV=1
COPY packages.R /tmp/packages.R
RUN Rscript /tmp/packages.R

FROM rocker/r-ver:${R_VERSION} AS runtime

ENV R_LIBS_SITE=/opt/r-library \
    R_LIBS_USER=/opt/r-library \
    R_ENVIRON_USER=/dev/null \
    R_PROFILE_USER=/dev/null

COPY --from=build /opt/r-library /opt/r-library

RUN apt-get update && apt-get install -y --no-install-recommends libuv1t64 libglpk40 libodbc2 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY plumber.R entrypoint.R ./
COPY R/ ./R/

RUN groupadd --system rrunner \
    && useradd --system --gid rrunner --home-dir /work --no-create-home \
       --shell /usr/sbin/nologin rrunner \
    && mkdir -p /work \
    && chown rrunner:rrunner /work \
    && chmod 700 /work

RUN find / -xdev -type f -perm /6000 -exec chmod a-s {} + 2>/dev/null; \
    rm -rf /var/lib/apt/lists/* \
           /usr/bin/apt* /usr/bin/dpkg* /etc/apt \
           /usr/lib/R/bin/INSTALL /usr/lib/R/bin/REMOVE \
           /usr/lib/gcc /usr/lib/R/bin/exec/R.bin.orig 2>/dev/null; true

USER rrunner
EXPOSE 8080

ENTRYPOINT ["/usr/local/bin/Rscript", "/app/entrypoint.R"]