using System;
using EnvDTE;

namespace VSProjTypeExtractorManaged
{
    public class VSProjTypeWorker
    {
        DTE dte;
        bool dteInstanciated = false;

        public VSProjTypeWorker()
        {
            dteInstanciated = false;
        }

        ~VSProjTypeWorker()
        {
            CleanUp();
        }

        public void CleanUp()
        {
            if (dteInstanciated)
            {
                dte.Solution.Close();
                dte.Quit();
                dteInstanciated = false;
            }
        }

        public string ExtractProjectTypeGuid(string projPath, int VS_MajorVersion)
        {
            string projTypeGuid = "";
            try
            {
                // load project by silently automating the Visual Studio installation specified by the major version
                if (!dteInstanciated)
                {
                    string VisualStudioDTEVerString = String.Format("VisualStudio.DTE.{0}.0", VS_MajorVersion);
                    Type visualStudioType = Type.GetTypeFromProgID(VisualStudioDTEVerString);
                    dte = Activator.CreateInstance(visualStudioType) as DTE;
                    dte.MainWindow.Visible = false;
                    dte.SuppressUI = true;
                    dte.UserControl = false;
                    dte.Solution.Create(Environment.GetEnvironmentVariable("TEMP"), "Dummy.sln");
                    dteInstanciated = true;
                }
                Project projLoaded = dte.Solution.AddFromFile(projPath);
                projTypeGuid = projLoaded.Kind.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n{0}\n", ex.ToString());
            }
            return projTypeGuid;
        }
    }
}
