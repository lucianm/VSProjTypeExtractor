using System;
using EnvDTE;

namespace VSProjTypeExtractorManaged
{
    public class VSProjTypeWorker
    {
        public static string ExtractProjectTypeGuid(string projPath, int VS_MajorVersion)
        {
            string projTypeGuid = "";
            try
            {
                // load project by silently automating the Visual Studio installation specified by the major version
                string VisualStudioDTEVerString = String.Format("VisualStudio.DTE.{0}.0", VS_MajorVersion); 
                Type visualStudioType = Type.GetTypeFromProgID(VisualStudioDTEVerString);
                DTE dte = Activator.CreateInstance(visualStudioType) as DTE;
                dte.MainWindow.Visible = false;
                dte.SuppressUI = true;
                dte.UserControl = false;
                dte.Solution.Create(Environment.GetEnvironmentVariable("TEMP"), "Dummy.sln");
                Project projLoaded = dte.Solution.AddFromFile(projPath);
                projTypeGuid = projLoaded.Kind.ToString();
                dte.Solution.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n{0}\n", ex.ToString());
            }
            return projTypeGuid;
        }
    }
}
