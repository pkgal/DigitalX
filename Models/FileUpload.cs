using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;


namespace DigitalX.Models
{
    public class FileUpload
    {
        HttpPostedFileBase fileToUpload;
        public int MaxFileSizeInMB { get; set; } =1; //Default Max File Sized = 1 MB
        public string FileMessage{ get; private set; }
        public string ModifiedFileName { 
            get
            {
                //return Path.GetFileNameWithoutExtension(fileToUpload.FileName) + "_" + DateTime.Now.ToString("yyyy-MM-dd h:mm tt") + Path.GetExtension(fileToUpload.FileName);
                return fileToUpload.FileName;
            }
        }

        public FileUpload(HttpPostedFileBase file) 
        {
            this.fileToUpload = file; 
        }

        public bool IsUploadedSuccessfully(string serverPath)
        {
            var supportedTypes = new[] { ".png", ".jpg", ".gif" };            
            string extension = Path.GetExtension(fileToUpload.FileName);
            bool success = false;

            if(fileToUpload.ContentLength<0)
            {
                this.FileMessage = "File does not exists";
            }
            else if(!supportedTypes.Contains(extension))
            {
                this.FileMessage = "Given image type is not supported";
            }
            else if((fileToUpload.ContentLength) > (MaxFileSizeInMB * 1024 * 1024))
            {
                this.FileMessage = "Uploaded file size should be less than " + MaxFileSizeInMB + " MB";
            }
            else
            {
                string path = Path.Combine(serverPath, this.ModifiedFileName);
                fileToUpload.SaveAs((path));

                if (File.Exists(path))
                    success= true;
                else
                {
                    FileMessage = "Some problem occured while uploading file";
                }
            }
            return success;
        }
    }
}