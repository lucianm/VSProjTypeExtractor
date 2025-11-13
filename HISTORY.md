# VSProjTypeExtractor

* v0.9.0.0 - published 2025.11.13:
    - improved logging, added loglevels configurable in the XML config file, console logging is always active and the default level is INFO;
    - replaced the delegate + RetryCall.Do pattern with an STA thread in the COM automation for extracting the project features,
    this also tolerates transient errors caused by incomplete "readiness" of VStudio when loading certain projects is retried by
    an exponential back off scheme to avoid hammering the DTE, increasing robustness of operation;

* v0.8.0.0 - published 2023.10.19:
    - switched configured Visual Studio version which is instrumented to 17 (2022);
    - solution version bumped to 17;
    - envdte reference replaced by Microsoft.VisualStudio.Interop;
    - PlatformToolset in unmanaged projects bumped to v143;
    - evaluating environment variable 'PROJTYPEXTRACT_VSVERSION' which takes precedence over the configured Visual Studio version (<major_version> in VsProjTypeExtractorManaged.xml);
    - added .editorconfig;

* v0.7.0.0 - published 2022.07.18:
    - switched configured Visual Studio version which is instrumented to 16 (2019);
    - added configuration possibility to temporarily show the Visual Studio UI during instrumentation, to be able to see messages,
    popups in case of problems during GUID extraction;
    - using .NET Framework v4.8 now;
    - few enhancements to the Generate_Packages.cmd script;

* v0.6.0.0 - published 2020.02.27:
    - improved locking in VSProjTypeExtractor.cpp;

* v0.5.0.0 - published 2020.02.11:
    - calling Vspte_GetProjData will fail also if project configuration / platform pairs could not be retrieved;

* v0.4.0.0 - published 2020.02.06:
    - removed all "convenience code" (around the `VspteModuleWrapper` in the
    [VSProjTypeExtractor.h](https://github.com/lucianm/VSProjTypeExtractor/blob/master/VSProjTypeExtractor/VSProjTypeExtractor.h)
    header for dynamic integration, as an alternative to statical linking, and just documenting it in the updated
    [README.md](https://github.com/lucianm/VSProjTypeExtractor/blob/master/README.md)), allowing for own implementations;
    - extended API to retrieve also config/platform pairs, dealocate array of such pairs from the new ExtractedProjData struct
    using the library's native runtime where it is also allocated;

* v0.3.0.0 - published 2019.11.18:
    - added logging (default configured off) and a XML configuration file [VsProjTypeExtractorManaged.xml](https://github.com/lucianm/VSProjTypeExtractor/blob/master/VSProjTypeExtractorManaged/VsProjTypeExtractorManaged.xml);
    - made retrieving project type Guid more robust, as at the very first project in a list of many loaded into the volatile
    solution it could happen that the automated Visual Studio instance underneath is still busy and throws
    System.Runtime.InteropServices.COMException with RPC_E_SERVERCALL_RETRYLATER, now we just retry more times after waiting
    inbetween (all configurable in the XML file);
    - updated [README.md](https://github.com/lucianm/VSProjTypeExtractor/blob/master/README.md);

* v0.2.0.0 - published 2019.11.16:
    - improved error handling;
    - disabled unicode;
    - fixed header;
    - tuned deployment helper script;
    - updated [README.md](https://github.com/lucianm/VSProjTypeExtractor/blob/master/README.md);

* v0.1.0.0 - published 2019.11.15:
    - provided a singleton `VspteModuleWrapper` in the
    [VSProjTypeExtractor.h](https://github.com/lucianm/VSProjTypeExtractor/blob/master/VSProjTypeExtractor/VSProjTypeExtractor.h)
    header for dynamic integration, as an alternative to statical linking, please consult the updated
    [README.md](https://github.com/lucianm/VSProjTypeExtractor/blob/master/README.md)
    - added this HISTORY.md file
    - added deployment helper script for generating release archives;

* v0.0.1.0 - published 2019.11.12:
    - initial version

© 2022 Lucian Muresan
