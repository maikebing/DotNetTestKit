﻿using CassiniDev;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DotNetTestkit
{
    public class EmbeddedServer: IDisposable
    {
        private Server server;

        private EmbeddedServer(Server server)
        {
            this.server = server;
        }

        public class Builder
        {
            private int port;
            private List<DirectoryMapping> virtualDirectories = new List<DirectoryMapping>();
            private TextWriter outputWriter;

            protected internal Builder(int port)
            {
                this.port = port;
            }

            public Builder WithVirtualDirectory(string virtualPath, string directoryPath)
            {
                virtualDirectories.Add(new DirectoryMapping(virtualPath, directoryPath));

                return this;
            }

            public Builder WithOutputCollectionTo(TextWriter output)
            {
                outputWriter = output;

                return this;
            }

            public EmbeddedServer Start()
            {
                var mainAppVirtualPath = virtualDirectories.First().VirtualPath;
                var mainAppPhysicalPath = virtualDirectories.First().PhysicalPath;

                var dirPath = Path.GetFullPath(mainAppPhysicalPath);

                if (!Directory.Exists(dirPath))
                {
                    throw new DirectoryNotFoundException(dirPath);
                }
                
                var server = new Server(port, mainAppVirtualPath, mainAppPhysicalPath);

                virtualDirectories.Skip(1).ToList().ForEach(additionalMapping =>
                {
                    server.RegisterAdditionalMapping(additionalMapping.VirtualPath, additionalMapping.PhysicalPath);
                });

                server.OutputWriter = outputWriter;

                server.Start();

                AppDomain.CurrentDomain.DomainUnload += (e, a) =>
                {
                    server.Dispose();
                };

                return new EmbeddedServer(server);
            }
        }

        public static Builder NewServer(int port)
        {
            return new Builder(port);
        }

        public void Dispose()
        {
            server.Dispose();
        }
    }

    class DirectoryMapping
    {
        public DirectoryMapping(string virtualPath, string physicalPath)
        {
            this.VirtualPath = virtualPath;
            this.PhysicalPath = physicalPath;
        }

        public string PhysicalPath { get; private set; }
        public string VirtualPath { get; private set; }
    }
}
