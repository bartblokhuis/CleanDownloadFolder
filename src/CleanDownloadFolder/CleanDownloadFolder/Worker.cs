using CleanDownloadFolder.Helpers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CleanDownloadFolder
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private static string DownloadFolderPath => KnownFolders.GetPath(KnownFolder.Downloads);
        public string OutputPath { get; set; } = "E:\\tempDownload";


        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (string.IsNullOrEmpty(DownloadFolderPath))
                throw new ApplicationException("Couldn't get the download folder path");
            
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                await CleanDownloadFolder();

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task CleanDownloadFolder()
        {
            var downloadFolderDirectory = new DirectoryInfo(DownloadFolderPath);
            var outputDirectory = new DirectoryInfo(OutputPath);

            if (!downloadFolderDirectory.Exists)
                throw new ApplicationException("Download folder doesn't exist");

            if (!outputDirectory.Exists)
                throw new ApplicationException("Output folder doesn't exist");

            var downloadFolderFiles = downloadFolderDirectory.GetFiles().Where(x => !x.Name.Equals("tempdesktop.ini") && x.CreationTimeUtc <= DateTime.UtcNow.AddDays(-1)).ToList();

            if (!downloadFolderFiles.Any())
                return;

            var firstDate = downloadFolderFiles.Min(x => x.CreationTime);
            var lastDate = downloadFolderFiles.Max(x => x.CreationTime);

            var tempFolderName = GetOutputFolderName(firstDate, lastDate);
            var tempFolderPath = OutputPath + "\\" + tempFolderName;

            var filesCopied = await CopyFilesToNewFolder(downloadFolderFiles, tempFolderPath);
            if(filesCopied != 0)
            {
                Notifier.Notify($"Copied {filesCopied} to the folder: {tempFolderName}");
            }
        }

        /// <summary>
        /// Get's the folder name
        /// </summary>
        /// <param name="minCreationDate"></param>
        /// <param name="maxCreationDate"></param>
        /// <returns></returns>
        private string GetOutputFolderName(DateTime minCreationDate, DateTime maxCreationDate)
        {
            var name = $"Downloads {minCreationDate.ToShortDateString()}-{maxCreationDate.ToShortDateString()}".Replace("/", "-");

            var outputDirectory = new DirectoryInfo(OutputPath);
            var outputDirectoryForDate = outputDirectory.GetDirectories().Where(x => x.Name.StartsWith(name)).ToList();

            if (outputDirectoryForDate.Any())
                name += $" ({outputDirectoryForDate.Count + 1})";

            outputDirectory.CreateSubdirectory(name);
            return name;
        }

        /// <summary>
        /// Copy the files to the new folder
        /// </summary>
        /// <param name="files"></param>
        /// <param name="tempFolderPath"></param>
        /// <returns></returns>
        private async Task<int> CopyFilesToNewFolder(IEnumerable<FileInfo> files, string tempFolderPath)
        {
            var tasks = new List<Task>();

            foreach (var file in files)
            {
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        file.CopyTo(tempFolderPath + "\\" + file.Name);
                        
                        if (File.Exists(tempFolderPath + "\\" + file.Name))
                            file.Delete(); //Ensure the file exists before deleting it from the download folder
                    }
                    catch (Exception e)
                    {
                        var message = e.Message;
                    }
                }));
            }

            await Task.WhenAll(tasks);
            return tasks.Count(x => x.IsCompletedSuccessfully);
        }
    }
}
