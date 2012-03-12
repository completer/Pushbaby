
Pushbaby - deployment without the labour pains
==============================================

In stark contrast to human childbirth, Pushbaby makes deploying your applications quite easy.

(1) Glides though firewalls.  
Forget FTP. No need to talk to Colin from IT. Pushbaby works on port 80. Like MSDeploy, but easy. 

(2) Secure by default, works first time.  
Pushbaby traffic is encrypted, and hardly needs very little setup.

(3) Real time progress.  
Pushbaby streams progress from your deployment script to your build server UI in realtime. With no setup!

How it works
------------

Pushbaby is a simple tool for server software deployment.

1. On the build server, Pushbaby.Client pushes your build to your destination server(s).
2. On the destination server, Pushbaby.Service executes your deployment script.
3. The standard output is streamed back to your build server through Pushbaby.Client.

Pushbaby *doesn't* help with specific deployment tasks like web server configuration. It just runs your bat file (or whatever).

How to use Pushbaby
-------------------

Put the Pushbaby.Client binaries folder on your build server.  
Put the Pushbaby.Service binaries folder on your destination server(s).

In the service config, set the values:
*   `SharedSecret` - an encryption key for secure communication over the network.
*   `DeploymentDirectory` - the path of the directory in which to place the uploaded file.
*   `ExecutableFile` - the path of the file to run when the upload is complete.

If you want multiple Pushbaby services running on the same destination machine, or you just want a non-default URI, you can set the value:
*   `UriPrefix` - the URI to listen on. The default value is `http://+:80/pushbaby/`. See [MSDN](http://msdn.microsoft.com/en-us/library/system.net.httplistenerprefixcollection.add.aspx) for more on the http.sys prefix format.

In the client config, set the value:
* `SharedSecret` - to the same as you used on the server.

To install Pushbaby.Service as a Windows Service
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

    Pushbaby.Client "D:\Builds\Foobler.1.0.0.123.zip" "http://172.16.0.70/pushbaby/"
 
 