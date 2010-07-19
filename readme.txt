THE AMQP Spring Extension project for .NET, Release 1.0.0 M1 (Date TBD)
--------------------------------------------------------------------
http://www.springsource.org/extensions/se-surf-net


1. INTRODUCTION

The 1.0.0 M1 release of Spring AMQP for NET contains

     * An AMQP implementation agnostic Message API for use across multiple implementations
     * A RabbitTemplate implementation for synchronous sending and receiving and supporting conversion to and from POJO to Rabbit's byte[] payload type.
     * A ConnectionFactory abstraction manages a single connection to the broker and an experimental implementation that supports a
       cache of Channels
     * A mutithreaded MessageListnerContainer supporting message driven POCOs.
     * MessageConverter implementations that support String and byte[].
     * Erlang Helper Library
     * RabbitMQ Administrative API

2. SUPPORTED .NET FRAMEWORK VERSIONS

Spring AMQP for .NET 1.0 supports .NET 2.0, 3.0 and 3.5.  Note, there are no specific .DLLs for .NET 3.5 as we do not use any features or libraries from that framework version.  

3. KNOWN ISSUES

None

4. RELEASE INFO

Release contents:

* "src" contains the C# source files for the framework
* "test" contains the C# source files for the test suite
* "bin" contains the distribution dll files
* "lib/net" contains common libraries needed for building and running the framework
* "lib/Rabbit" contains the Rabbit dlls
* "doc" contains reference documentation, MSDN-style API help, and the Spring AMQP for .NET xsd.
* "examples" contains sample applications.

debug build is done using /DEBUG:full and release build using /DEBUG:pdbonly flags.

The VS.NET solution for the framework and examples are provided.

Latest info is available at the public website: http://www.springsource.org/extensions/se-surf-net

Spring AMQP for .NET is released under the terms of the Apache Software License (see license.txt).


5. DISTRIBUTION DLLs

The "bin" directory contains the following distinct dll files for use in applications. Dependencies are those other than on the .NET BCL.
6. WHERE TO START?

Documentation can be found in the "docs" directory:
* The Spring for AMQP reference documentation

Documented sample applications can be found in "examples":

7. How to build

VS.NET
------
The is a solution file for different version of VS.NET

* Spring.Messaging.Amqp.2008.sln for use with VS.NET 2008

8. Support

The user forums at http://forum.springframework.net/ are available for you to submit questions, support requests,
and interact with other Spring.NET users.

Bug and issue tracking can be found at https://jira.springsource.org/browse/AMQPNET

A Fisheye repository browser is located at

To get the sources, check them out at the git repository at git@git.springsource.org:spring-amqp/spring-amqp-net.git

We are always happy to receive your feedback on the forums. If you think you found a bug, have an improvement suggestion
or feature request, please submit a ticket in JIRA (see link above).

9. Acknowledgements

InnovaSys Document X!
---------------------
InnovSys has kindly provided a license to generate the SDK documentation and supporting utilities for
integration with Visual Studio.






