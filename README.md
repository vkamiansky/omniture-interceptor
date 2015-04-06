# omniture-interceptor
A tool that opens expressen pages in two different test environments and juxtaposes the parameters of Omniture requests coming from the pages with same relative paths, logs the differences, caught requests.

The tool works like this:

1) The file called **testpath.txt** is read from the app's folder to get relative paths of the pages under test;

Example file content:

```
nyheter/plus-folj-presidentvalet---direkt-pa-expressen/
nyheter/utmaningarna-for-framtidens-sverigey/
nyheter/tag-star-stilla-i-skane-efter-olycka-i-hoor/
nyheter/anton-17-tar-traktorn-till-skolan-i-klippan/
nyheter/live-temperaturen-kommer-stiga-i-ratten/
nyheter/sra-quiz/
nyheter/tva-friade-for-utlandsfiske/

sport/
```

2) *Interceptor.exe.config* (**app.config**) is used to read addresses of the current and the candidate environments;

Example environments config section:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
...
  <appSettings>
    <add key="current" value="http://exp-cms-test2.bonnierdigitalservices.se/"/>
    <add key="candidate" value="http://exp-cms-test6.bonnierdigitalservices.se/"/>
  </appSettings>
</configuration> 
```

4) The Omniture requests capturing process is started by pressing a key; 

5) Pages will be opened automatically with an interval of 3 seconds. When a request is captured an echo message is sent to console. As soon as all the pages are opened the respective message will be printed out and you will be able to stop the capturing process by pressing a key when ready.

6) The differences between parameters of requests captured for the candidate and the current environments as well as all the caught requests will be saved to the log file. 
