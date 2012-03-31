
Pushbaby - deployment without the labour pains
==============================================

In stark contrast to human childbirth, Pushbaby makes deploying your applications quite easy.

Pushbaby is a simple tool for server software deployment on Windows.

If you are deploying *more than a just a website* or find it a *pain to connect* to your production server, you might find it useful.

**(1) Glides though firewalls**  
Forget FTP. No need to talk to Colin from IT. Pushbaby works on port 80.

**(2) Mindless configuration**  
A simple alternative to MSDeploy.

**(3) Progress in real time**  
Don't hang your build server. Your deployment script output is streamed back as it runs.

Pushbaby is also very fast, and secure without SSL (at your own risk - see disclaimer).

How it works
------------

1. On your build server, Pushbaby.Client pushes the build output to your destination server(s).
2. On the destination server, Pushbaby.Service executes your own custom deployment script.
3. The standard output is streamed back to your build server through Pushbaby.Client.

**Additional features** include payload compession, parallel destination uploading and "snaked" deployment directories for atomic activation.

Pushbaby **doesn't help** with specific deployment tasks like web server configuration. It just executes your bat file (or whatever) on the destination server.

How to use Pushbaby
-------------------

Put the Pushbaby.Client folder on your build server.  
Put the Pushbaby.Service folder on your destination server(s) and install (see below).  
You can ignore the Pushbaby.Server folder unless you want to quickly run the server as a console app.

How to prepare the service
--------------------------

In the `Pushbaby.Service.exe.config`, set the values:

- `uri` - the URI to listen on. See MSDN for the http.sys [prefix format](http://msdn.microsoft.com/en-us/library/system.net.httplistenerprefixcollection.add.aspx).
- `sharedSecret` - an encryption key for secure communication over the network.
- `deploymentDirectory` - the path of the directory in which to place the payload.
- `executableFile` - the path of the file to run when the upload is complete. 

    <pushbaby>
      <endpoints>
        <endpoint uri="http://+:80/pushbaby/"
                  sharedSecret="some-secure-key"
                  deploymentDirectory="c:\deployments"
                  executableFile="c:\deployments\deployment-script.bat" />
      </endpoints>
    </pushbaby>

The path to the uploaded payload is passed as the only argument to the executable file.

Open an Administrator command window.  
    cd C:\Windows\Microsoft.NET\Framework\v4.0.30319
    installutil {path-to-Pushbaby.Service.exe}

In the Windows Services UI:
Set the logon of the service to a user with necessary (probably Administrator) rights. 
Start the service.

To uninstall use the `/u` switch.
 
How to use the client
---------------------

In the `Pushbaby.Client.exe.config`, set the value:

- `SharedSecret` - to the same as you used on the server.

Call Pushbaby as the last step in your build process.

    Pushbaby.Client {path-of-file-or-directory-to-upload} {destination-1} [{destination-2} ...]
    
For example:

    Pushbaby.Client "D:\Builds\Foobler.1.2.0.1234" "http://www.example.com/pushbaby/"

FAQ
---

**How do I know the service is running correctly?**
Open the endpoint URI in your web browser.

**Can I deploy more than one application to the same server?**
Yes, you can specifiy multiple endpoints if necessary.

    <pushbaby>
      <endpoints>
        <endpoint uri="http://+:80/pushbaby-my-app-beta/" ... />
        <endpoint uri="http://+:80/pushbaby-my-app-live/" ... />
      </endpoints>
    </pushbaby>
 
Disclaimer
----------
This code exists "as-is" on Github.com.  It was designed to solve a real problem and as an exercise in network programming.
The author is not a security expert and the code has not been reviewed by a security professional.

Praise for Pushbaby
-------------------

> This is a terrible idea.

*Eric Lippert*

> You should of used SSH.    

*Colin from IT*
 