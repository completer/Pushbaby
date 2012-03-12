
Pushbaby - deployment without the labour pains
==============================================

In stark contrast to human childbirth, Pushbaby makes deploying your applications quite easy.

Pushbaby is a simple tool for server software deployment on Windows. If you are deploying **more than a just a website** or find it a **pain to  connect** to your production server, you might find it useful.

**(1) Glides though firewalls**  
Forget FTP. No need to talk to Colin from IT. Pushbaby works on port 80.

**(2) Works first time, secure by default**  
A simple alternative to MSDeploy, setup is a doddle.

**(3) See progress in real time**  
Doesn't just hang your build server. Your deployment script output is interactively streamed back as it executes.

How it works
------------

1. On the build server, Pushbaby.Client pushes your build output to your destination server(s).
2. On the destination server, Pushbaby.Service executes your deployment script.
3. The standard output is streamed back to your build server through Pushbaby.Client.

Pushbaby **doesn't help** with specific deployment tasks like web server configuration. It just executes your bat file (or whatever).

How to use Pushbaby
-------------------

Put the Pushbaby.Client binaries folder on your build server.  
Put the Pushbaby.Service binaries folder on your destination server(s).

In the **service** app.config, set the values:

- `SharedSecret` - an encryption key for secure communication over the network.
- `DeploymentDirectory` - the path of the directory in which to place the uploaded file.
- `ExecutableFile` - the path of the file to run when the upload is complete.

If you want multiple Pushbaby services running on the same destination machine (or you simply want a non-default URI), set the value:

- `UriPrefix` - the URI to listen on. The default value is `http://+:80/pushbaby/`. See MSDN for the http.sys [prefix format](http://msdn.microsoft.com/en-us/library/system.net.httplistenerprefixcollection.add.aspx).

In the **client** app.config, set the value:

- `SharedSecret` - to the same as you used on the server.

How to install the service as a Windows Service
------------------------------------------------

Open an Administrator command window.  
`cd` to the Pushbaby.Service binaries folder.

    C:\Windows\Microsoft.NET\Framework\v4.0.30319\installutil Pushbaby.Service.exe

To uninstall use the `/u` switch.

The service needs to run with elevated privileges to listen although see bottom of
http://stackoverflow.com/questions/4019466/httplistener-access-denied-c-sharp-windows-7

Usage
-----

Call Pushbaby as the last step in your build process.

    Pushbaby.Client {path-of-file-to-upload} {destination-1} [{destination-2} ...]
    
For example:

    Pushbaby.Client "D:\Builds\Foobler.1.0.0.123.zip" "http://www.example.com/pushbaby/"
 
 