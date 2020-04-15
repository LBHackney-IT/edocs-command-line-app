using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PCDCLIENTLib = Hummingbird.DM.Server.Interop.PCDClient;
using dotenv.net;
using dotenv.net.Utilities;


namespace edocs_test_console_app
{
    class Program
    {
        static void Main(string[] args)
        {
            DotEnv.AutoConfig();
            var library = "CDM";
            var docNumber = "8671310";
            var PCDLogin = new PCDCLIENTLib.PCDLogin();

            var username = Environment.GetEnvironmentVariable("EDOCSUSERNAME");
            var password = Environment.GetEnvironmentVariable("EDOCSPASSWORD");


            var rc = PCDLogin.AddLogin(0, library, username, password);

            rc = PCDLogin.Execute();

            if (rc != 0)
            {
                throw new SystemException();
            }

            var dst = PCDLogin.GetDST();
            Console.WriteLine("The dst is:" + dst);

            var obj = new PCDCLIENTLib.PCDSearch();
            obj.SetDST(dst);
            obj.AddSearchLib(library);
            obj.SetSearchObject("DEF_PROF");
            obj.AddSearchCriteria("DOCNUMBER", docNumber);
            obj.AddReturnProperty("PATH");
            obj.AddReturnProperty("DOCNAME");

            rc = obj.Execute();
            if (rc != 0)
            {
                Console.WriteLine(obj.ErrDescription);
                throw new SystemException();
            }

            obj.SetRow(1);


            var docname = obj.GetPropertyValue("DOCNAME");
            Console.WriteLine("Doc name:" + docname);
            obj.GetPropertyValue("PATH");
            obj.ReleaseResults();


            var sql = new PCDCLIENTLib.PCDSQL();
            sql.SetDST(dst);
            rc = sql.Execute("SELECT PATH FROM DOCSADM.COMPONENTS WHERE DOCNUMBER = " + docNumber);

            if (rc != 0)
            {
                Console.WriteLine(sql.ErrDescription);
                throw new SystemException();
            }

            sql.SetRow(1);
            var path = sql.GetColumnValue(1);
            Console.WriteLine("The path is:" + path);
            var tokens = path.Split('.');

            var fileType = tokens[tokens.Length - 1].ToLower();
            Console.WriteLine("File type:" + fileType);

            obj = new PCDCLIENTLib.PCDSearch();
            obj.SetDST(dst);
            obj.AddSearchLib(library);
            obj.SetSearchObject("cyd_cmnversions");
            obj.AddSearchCriteria("DOCNUMBER", docNumber);
            obj.AddOrderByProperty("VERSION", 0);
            obj.AddReturnProperty("VERSION");
            obj.AddReturnProperty("VERSION_ID");

            rc = obj.Execute();


            if (rc != 0)
            {
                Console.WriteLine(sql.ErrDescription);
                throw new SystemException();
            }

            obj.SetRow(1);
            var version = obj.GetPropertyValue("VERSION");
            var versionId = obj.GetPropertyValue("VERSION_ID");
            Console.WriteLine("Version: $version Version ID: " + versionId);
            obj.ReleaseResults();

            string ver = "" + versionId;

            var getobj = new PCDCLIENTLib.PCDGetDoc();
            getobj.SetDST(dst);
            getobj.AddSearchCriteria("%TARGET_LIBRARY", library);
            getobj.AddSearchCriteria("%VERSION_ID", ver);
            getobj.AddSearchCriteria("%DOCUMENT_NUMBER", docNumber);
            rc = getobj.Execute();

            if (rc != 0)
            {
                Console.WriteLine(getobj.ErrDescription);
                throw new SystemException();
            }


            getobj.NextRow();
            
            var filename = docNumber + "." + fileType;
            PCDCLIENTLib.PCDGetStream objPCDGetStream = (PCDCLIENTLib.PCDGetStream)getobj.GetPropertyValue("%CONTENT");
            int nbytes = (int)objPCDGetStream.GetPropertyValue("%ISTREAM_STATSTG_CBSIZE_LOWPART");

            using (Stream to = new FileStream(filename, FileMode.OpenOrCreate))
            {
                int readCount;
                byte[] buffer = new byte[nbytes];
                buffer = (byte[])objPCDGetStream.Read(nbytes, out readCount);
                to.Write(buffer, 0, readCount);
            }

            Console.Read();

        }

    }

}
