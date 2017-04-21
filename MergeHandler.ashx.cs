using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using C1.Silverlight;

namespace CoreX
{
    /// <summary>
    /// Summary description for $codebehindclassname$
    /// </summary>
    [WebService(Namespace = "http://www.marcseidl.eu/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    public class MergeHandler : IHttpHandler
    {

        /*
         *  --------------------------------------------------------------------
         *  This handler process any quantity of files splitted in smaller parts
         *  using the Multipart/Post format that is standard in the Browsers
         *  --------------------------------------------------------------------
         */
        public void ProcessRequest(HttpContext context)
        {
            try
            {
                // get custom parameters
                string parameters = context.Request.Params["parameters"];

                // get the uploaded file, and calculates the full path to save it in the server
                HttpPostedFile file = context.Request.Files[0];
                string serverFileName = C1UploaderHelper.GetServerPath(context.Server, file.FileName);

                // get parts parameters (used to upload a file broken into small parts)
                int partCount = int.Parse(context.Request.Params["partCount"]);
                int partNumber = int.Parse(context.Request.Params["partNumber"]);

                // process this new small part
                if (C1UploaderHelper.ProcessPart(context, file.InputStream, serverFileName, partCount, partNumber))
                {
                    // write the url of the uploaded file into the response
                    string url = C1UploaderHelper.GetUploadedFileUrl(context, "MergeHandler.ashx", file.FileName);
                    context.Response.Write(url);
                }
                else
                {
                    C1UploaderHelper.WriteError(context, C1UploaderHelper.ERROR_MESSAGE);
                }
            }
            catch (Exception exc)
            {
                C1UploaderHelper.WriteError(context, exc.Message);
                context.Response.End();
            }
        }


        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}
