# omniture-interceptor
Omniture Interceptor is a regression testing tool designed for the Expressen statistics functionality.
It runs a specified set of Expressen pages through a number of test scenarios and tracks the discrepancies between the  Omniture requests as formed by two different environments. 

The tool has been tested to run properly on a Windows 7 machine with Firefox installed and without network access restrictions through firewall or otherwise applied.

The tool uses Selenium WebDriver to carry out it's automated testing scenarios.

1) The relative paths of the pages called during the tests are specified in **testpath.txt**.;

Example file content:

```
tv/nyheter/forlorade-halva-skallen-nar-han-korde-drogpaverkad/
nyheter/plus-folj-presidentvalet---direkt-pa-expressen/
nyheter/utmaningarna-for-framtidens-sverigey/
nyheter/tag-star-stilla-i-skane-efter-olycka-i-hoor/
nyheter/anton-17-tar-traktorn-till-skolan-i-klippan/
nyheter/live-temperaturen-kommer-stiga-i-ratten/
nyheter/sra-quiz/
nyheter/tva-friade-for-utlandsfiske/
nyheter/jag-at-sakert-ett-par-kilo-socker-i-veckan/

sport/
sok/?q=lyssna
```

2) The base addresses for the two environments - the current and the candidate - are specified in **Interceptor.exe.config**;

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

3) The testing process is started by running **Interceptor.exe** and pressing a key; 

4) The differences between parameters of the requests captured for the candidate and the current environments as well as all the caught requests will be saved to the **log-file.txt**. 
