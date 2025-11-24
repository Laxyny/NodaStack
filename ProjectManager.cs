using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;

namespace NodaStack
{
    public class ProjectManager
    {
        public string ProjectsPath { get; private set; }

        public ProjectManager()
        {
            // Initial path loaded from config (we'll need to inject config or read it)
            // For now, default relative "www". 
            // Better approach: Read config directly here or allow setting path
            var configManager = new ConfigurationManager();
            ProjectsPath = configManager.GetConfiguration().Settings.ProjectsPath;
            
            if (string.IsNullOrWhiteSpace(ProjectsPath) || ProjectsPath == "www")
            {
                ProjectsPath = Path.Combine(Directory.GetCurrentDirectory(), "www");
            }
            
            EnsureProjectsDirectory();
        }

        public void UpdateProjectsPath(string newPath)
        {
            if (string.IsNullOrWhiteSpace(newPath)) return;
            
            ProjectsPath = newPath;
            EnsureProjectsDirectory();
        }

        public string GetProjectsPath()
        {
            return ProjectsPath;
        }

        private void EnsureProjectsDirectory()
        {
            if (!Directory.Exists(ProjectsPath))
            {
                Directory.CreateDirectory(ProjectsPath);
                CreateDemoProject();
            }
        }

        private void CreateDemoProject()
        {
            var demoPath = Path.Combine(ProjectsPath, "demo");
            Directory.CreateDirectory(demoPath);

            var indexContent = @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>NodaStack - Demo Project</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 40px; background: #f5f5f5; }
        .container { max-width: 800px; margin: 0 auto; background: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
        h1 { color: #333; border-bottom: 3px solid #4CAF50; padding-bottom: 10px; }
        .status { background: #e8f5e8; padding: 15px; border-radius: 5px; border-left: 4px solid #4CAF50; margin: 20px 0; }
        .info { background: #e3f2fd; padding: 15px; border-radius: 5px; border-left: 4px solid #2196F3; margin: 20px 0; }
        code { background: #f5f5f5; padding: 2px 6px; border-radius: 3px; font-family: 'Courier New', monospace; }
    </style>
</head>
<body>
    <div class=""container"">
        <h1>🚀 NodaStack Demo Project</h1>
        
        <div class=""status"">
            <strong>✅ Project is working!</strong><br>
            Your NodaStack environment is configured correctly.
        </div>
        
        <div class=""info"">
            <h3>📂 Project Location</h3>
            <p>This project is located in: <code>www/demo/</code></p>
            
            <h3>🔧 Getting Started</h3>
            <ul>
                <li>Edit <code>index.html</code> to customize this page</li>
                <li>Add PHP files for dynamic content</li>
                <li>Create subdirectories for organization</li>
                <li>Access via: <code>http://localhost:8080/demo/</code></li>
            </ul>
            
            <h3>🌐 Available Services</h3>
            <ul>
                <li><strong>Apache:</strong> <code>http://localhost:8080</code></li>
                <li><strong>PHP:</strong> <code>http://localhost:8000</code></li>
                <li><strong>phpMyAdmin:</strong> <code>http://localhost:8081</code></li>
            </ul>
        </div>
        
        <p><em>Happy coding with NodaStack! 🎉</em></p>
    </div>
</body>
</html>";

            File.WriteAllText(Path.Combine(demoPath, "index.html"), indexContent);

            var phpContent = @"<?php
echo ""<h2>PHP is working! 🐘</h2>"";
echo ""<p>Current time: "" . date('Y-m-d H:i:s') . ""</p>"";
echo ""<p>PHP Version: "" . PHP_VERSION . ""</p>"";
?>";

            File.WriteAllText(Path.Combine(demoPath, "test.php"), phpContent);
        }

        public List<ProjectInfo> GetProjects()
        {
            var projects = new List<ProjectInfo>();

            if (!Directory.Exists(ProjectsPath))
                return projects;

            foreach (var directory in Directory.GetDirectories(ProjectsPath))
            {
                var projectName = Path.GetFileName(directory);
                var hasIndex = File.Exists(Path.Combine(directory, "index.html")) ||
                              File.Exists(Path.Combine(directory, "index.php"));

                var fileCount = Directory.GetFiles(directory, "*", SearchOption.AllDirectories).Length;
                var lastModified = Directory.GetLastWriteTime(directory);

                projects.Add(new ProjectInfo
                {
                    Name = projectName,
                    Path = directory,
                    HasIndexFile = hasIndex,
                    FileCount = fileCount,
                    LastModified = lastModified,
                    ApacheUrl = $"http://localhost:8080/{projectName}/",
                    PhpUrl = $"http://localhost:8000/{projectName}/"
                });
            }

            return projects.OrderBy(p => p.Name).ToList();
        }

        public bool CreateProject(string projectName)
        {
            if (string.IsNullOrWhiteSpace(projectName))
                return false;

            projectName = projectName.Trim().ToLower();
            projectName = System.Text.RegularExpressions.Regex.Replace(projectName, @"[^a-z0-9\-_]", "");

            if (string.IsNullOrEmpty(projectName))
                return false;

            var projectPath = Path.Combine(ProjectsPath, projectName);

            if (Directory.Exists(projectPath))
                return false;

            try
            {
                Directory.CreateDirectory(projectPath);

                var indexContent = $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{projectName} - NodaStack Project</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 40px; background: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        h1 {{ color: #333; }}
        .welcome {{ background: #e8f5e8; padding: 15px; border-radius: 5px; border-left: 4px solid #4CAF50; }}
    </style>
</head>
<body>
    <div class=""container"">
        <h1>🚀 {projectName}</h1>
        <div class=""welcome"">
            <strong>Welcome to your new NodaStack project!</strong><br>
            Start editing this file to build your application.
        </div>
        <p>Project created on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
    </div>
</body>
</html>";

                File.WriteAllText(Path.Combine(projectPath, "index.html"), indexContent);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool DeleteProject(string projectName)
        {
            var projectPath = Path.Combine(ProjectsPath, projectName);

            if (!Directory.Exists(projectPath))
                return false;

            try
            {
                Directory.Delete(projectPath, true);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void OpenProjectInExplorer(string projectName)
        {
            var projectPath = Path.Combine(ProjectsPath, projectName);

            if (Directory.Exists(projectPath))
            {
                Process.Start("explorer.exe", projectPath);
            }
        }

        public void OpenProjectInBrowser(string projectName, bool useApache = true)
        {
            var url = useApache ? $"http://localhost:8080/{projectName}/" : $"http://localhost:8000/{projectName}/";

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch
            {
                // Ignore errors
            }
        }

        public void OpenProjectsDirectory()
        {
            if (Directory.Exists(ProjectsPath))
            {
                Process.Start("explorer.exe", ProjectsPath);
            }
        }
    }

}