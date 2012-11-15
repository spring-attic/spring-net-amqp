Spring AMQP for .NET
====================

This project provides support for using Spring and .NET with [AMQP](http://www.amqp.org/), and in particular [RabbitMQ](http://www.rabbitmq.com/).

## NuGet Packages

To use Spring AMQP for .NET in your applications, you should use the NuGet Packages found at the NuGet Gallery:

* http://nuget.org/packages/Spring.Messaging.Amqp
* http://nuget.org/packages/Spring.Messaging.Amqp.Rabbit

## Checking out and Building

To check out the project from [GitHub](https://github.com/SpringSource/spring-net-amqp) and build from source using [MSBuild](http://msdn.microsoft.com/en-us/library/vstudio/dd393574.aspx), do the following from a command prompt:

    git clone git://github.com/SpringSource/spring-net-amqp.git
    cd spring-net-amqp
    build

## Using VS2010

* Open Spring.Messaging.Amqp.2010-40.sln in VS 2010
* Build -> Build Solution (or F6)

## Supported .NET Framework Versions

Spring AMQP for .NET supports .NET 4.0 and 4.5. 

## Known Issues

* Retry functionality that exists in Spring AMQP (for Java) has not been ported yet, because Spring Retry functionality is not yet available for .NET. Development of Spring Retry for .NET will occur here: https://github.com/springsource/spring-net-retry

Check the JIRA site for the most up to date information: https://jira.springsource.org/browse/AMQPNET

## Changelog

Lists of issues addressed per release can be found in [JIRA](https://jira.springsource.org/browse/AMQPNET#selectedTab=com.atlassian.jira.plugin.system.project%3Aversions-panel).

## Additional Resources

* [Spring AMQP for .NET Homepage](http://www.springframework.net/amqp)
* [Spring AMQP for .NET Source](http://github.com/SpringSource/spring-net-amqp)
* [Spring AMQP for .NET Samples](http://github.com/SpringSource/spring-net-amqp-samples)
* [Spring AMQP (Java + .NET) Forum](http://forum.springsource.org/forumdisplay.php?f=74)
* [Stack Overflow](http://stackoverflow.com/questions/tagged/spring-net-amqp)

## Contributing to Spring AMQP for .NET

Here are some ways for you to get involved in the community:

* Get involved with the Spring community on the Spring Community Forums.  Please help out on the [forum](http://forum.springsource.org/forumdisplay.php?f=74) by responding to questions and joining the debate.
* Create [JIRA](https://jira.springsource.org/browse/AMQPNET) tickets for bugs and new features and comment and vote on the ones that you are interested in.  
* Github is for social coding: if you want to write code, we encourage contributions through pull requests from [forks of this repository](http://help.github.com/forking/).  If you want to contribute code this way, please reference a JIRA ticket as well covering the specific issue you are addressing.
* Watch for upcoming articles on Spring by [subscribing](http://www.springsource.org/node/feed) to springframework.org

Before we accept a non-trivial patch or pull request we will need you to sign the [contributor's agreement](https://support.springsource.com/spring_committer_signup).  Signing the contributor's agreement does not grant anyone commit rights to the main repository, but it does mean that we can accept your contributions, and you will get an author credit if we do. Active contributors might be asked to join the core team, and given the ability to merge pull requests.

## Acknowledgements

#### Innovasys Document! X

Innovsys has kindly provided a license to generate the SDK documentation and supporting utilities for integration with Visual Studio.

## License

Spring AMQP for .nET is released under the terms of the Apache Software License Version 2.0 (see license.txt).





