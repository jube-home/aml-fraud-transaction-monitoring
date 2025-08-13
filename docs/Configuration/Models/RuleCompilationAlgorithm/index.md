---
layout: default
title: Rule Compilation Algorithm
nav_order: 19
parent: Models
grand_parent: Configuration
---

🚀Speed up implementation with hands-on, face-to-face [training](https://www.jube.io/jube-training) from the developer.

# Rule Compilation Algorithm
Jube relies on the injection of VB .Net code which is subsequently complied by reflection in the .net core runtime.  While this allows for extremely fast rules to be created it does expose security issues in the possibility of code injection.  This code injection could be sinister given the breadth of the .net API (although the Jube instance should be run with the least privileges, not needing to perform any disk IO or operating system level interaction beyond logging, and even logging can be offloaded via syslog).

To ensure that malicious code cannot be been injected, an integrity check on the all code created by the user is performed before compile on creation and in the background during model synchronisation.  In the event that the code does not pass an integrity check, it would be bypassed, with ERROR level being written to the logs.

All tokens inside a rule must be registered in the RuleScriptToken table in the database:

```sql
select * from "RuleScriptToken"
```

There are some default tokens which are embedded into the parser already, but they support only the most basis logic statements and object names from model invocation. The following tokens are hard coded:

{"Return","If","Then","End If","False","True","Payload","Abstraction","Activation","Select","Case","End Select","Contains","Sanction","KVP","List","TTLCounter","String","Double","Integer","DateTime","CType","Boolean","Data","Calculation","Not"}

The parse function takes a string containing a VB .Net code fragment and performs several steps to parse for integrity.  For the purposes of this example,  the following rule will be parsed for integrity:

```vb
If Payload.Name = "Richard" Then
  Matched = True
End If
```

The first step of the parse is to break the rule into its component lines on CrLf, Cr or Lf.  Each line will be parsed one by one.

For the line, enclosed strings will be removed as they are allowed to contain a multitude of data.  In this example,  "Richard" is an enclosed string.  The soft parser will now see the line as follows:

```vb
If Data() = Then
```

The soft parse breaks the line into tokens, to check that the tokens exist in the registry. The line is split based upon the following array of allowed characters:

{",", " ", "(", ")", "=", ">", "<", ">=", "<=", "<>", ".", "_","+","-","/","*","&"}

The soft parser will now see an array as follows:

{"If","Payload","Then","End","If"}

Each token will be checked for integrity.  Firstly,  the token will be tested to see if it is numeric, and if so, no further integrity checking on that token will take place. Assuming that the token is not numeric, it will be validated against the list of allowed tokens. The base assumption is that the rule string is not valid and all tokens must be found in order for the line to be considered as being valid.

There is a curious case of valid language tokens such a "End If" which is logically a single token, but would be read as two tokens.  As seen above, it is possible to store such logical tokens in the registry.

For each token stored in the database or hardcoded, a test token will be taken and a match will be sought (i.e. looking one by one, for each token) against the registry of tokens.  If a match is found,  the integrity of that token will be declared valid,  and it will move onto the next token.

Any token that does not find a corresponding match or entry in the registry will be declared invalid, which is enough for the entire integrity of the rule text to be declared invalid, although the routine will continue to run, reporting out all problematic tokens to the error logs.

To deal with the unusual case where a token in the registry is separated by a space,  depending on the number of spaces in the token registry entry,  the test token will be reconstructed for that same number of subsequent tokens awaiting test (for example End,  which is not allowed,  is reconstructed to End If).  This is because to allow End could allow the fatal termination of the application to be injected, yet End If is innocent.

If for any reason any token parse fails, the parse would will return false, which will in most areas of the system cause that code not to be complied in reflection.  If for any reason rules are not working a expected, it is suggested to check the logs to look for any soft parse failures at ERROR level,  as this can be a common cause of rules not working as desired.

The premise of the Jube is that it is comprised of hundreds,  if not thousands of rules,  which are intended to match upon data processed in real-time.  One of the reasons that the Jube is so fast in processing, despite the number of rules required of processing, is that when a rule is created,  it is compiled to native code and as such runs about as fast as any low level .net programming function.

Compiling code dynamically is extraordinarily expensive and it is thunderously slow, hence cannot be relied upon in a real-time process. A process has been developed to perform compilation in the background while ensuring it can be referenced real-time as if it were - almost - a native function of the .Net platform,  hence extraordinarily fast.

The Jube uses .Net reflection and the language is VB.net (as this is more intuitive than C#,  although Jube is written in C#). The process of compilation is as follows:

* For every rule,  a class is created that makes references to several third party DLL's and;
* For the newly written class code,  a function of sub routines is created and;
* The rules are invariably very small fragments that are intended to sit inside the newly created subroutine, as such the code is embedded.
* A check is made to see if the class code already exists,  in a compiled state, in the compiled hash cache.  This step ensures that there is no duplication in rules in the applications memory as if it already exists in a compiled state,  it will simply be referenced rather than recompiled.
* In the absence of class code already existing in memory, it will be compiled to an assembly and;
* A delegate will be attached such that it provides recall performance not dissimilar to that of a native .Net function.
* Upon successful compilation,  the assembly will be added to the compiled hash cache.

One of the legacy weaknesses of the .Net core when dealing with compiled code is that while assemblies can be compiled and loaded dynamically, they cannot be unloaded.  It follows that a compiled assembly,  even if not being used,  will exist in the applications memory until the next restart.  The compiled rules take up a negligible amount of space in memory, but it is worth bearing in mind as a explanation for shallow memory leak over a long period of time.  In order to reduce the impact of this memory leak, a compiled hash cache is used to ensure that an assembly is only ever created the once in the lifetime of the application:

* When code is created dynamically,  as described above,  that code is hashed using MD5 to create a digest.
* This digest is the key to the assembly cache and upon successful compilation of the code into an assembly,  that assembly is stored as the value of a dictionary entry.
* In all cases of dynamic compilation the compiled hash case will be referenced to see if the assembly already exists such to avoid expensive recompilation and memory leak.

Henceforth,  in the use of the Assembly Cache,  where rule criteria is often in common,  the memory used by assemblies can often be reduced.

The following rules and scripts are subject to the compilation process and are the result of rules being created in the user interface as VB .Net code fragments:

* Abstraction Rules.
* Activation Rules.
* Gateway Rules.
* Inline Functions.
* Abstraction Calculations.

The following classes are created as much more advanced, and flexible, complete class stored in the database. The creation of these code fragments are documented separately,  although it suffices at this stage to explain that code can be freestyle subject to it conforming to an interface specification.

* Inline Scripts which exist as an entire class,  including Import statements,  implementing a specific interface.

Any compilation errors will be written to the logs as ERROR level, detailing the compiler errors.