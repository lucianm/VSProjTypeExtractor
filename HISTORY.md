# VSProjTypeExtractor

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

© 2020 Lucian Muresan
