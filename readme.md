#PowerShellAsync
base class for async-enabled PowerShell Cmdlets.

When you build PowerShell Cmdlets, it is required that calls to WriteObject, WriteVerbose, WriteWarning, etc be originated from BeginProcessing/ProcessRecord/EndProcessing method on __the main thread__!.

With a general move towards async code, it's become hard to use newer libraries inside the traditional PowerShell Cmdlets. Up until now :)

With 3 easy steps you can take advantage of the async programming:
 
1. Install PowerShellAsync from nuget.org
  * ``PM> Install-Package PowerShellAsync``


2. Replace base class of your Cmdlet from _PSCmdlet_ to _AsyncCmdlet_
  * ``[Cmdlet("Test", "SomethingCool")]
      public class TestSomethingCool : AsyncCmdlet``


3. Replace BeginProcessing/ProcessRecord/EndProcessing methods with their xxxAsync counterparts
  * ``protected override async Task ProcessRecordAsync()``


4. Enjoy!!!



## Contributors

  * [Vladimir Yangurskiy](https://github.com/ttrider) 
  * [pgrefviau](https://github.com/pgrefviau) 

# Installation

To install PowerShellAsync, run the following command in the Package Manager Console

``` 
PM> Install-Package PowerShellAsync
```

# Examples

please take a look at PowerShellAsyncExamples project, it contains an demo Cmdlet that can execute SQL statement on multiple servers takeing advantage of async API.

# Contact


[Vladimir (software@ttrider.com)](mailto:software@ttrider.com)


# License

[MIT](./LICENSE)
