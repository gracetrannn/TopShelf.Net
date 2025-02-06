// Copyright 2007-2012 Chris Patterson, Dru Sellers, Travis Smith, et. al.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
using System;
using ElmahCore; // Ensure the correct using directives for ElmahCore functionality

using System.Collections.Generic;
public class SimpleErrorLog : ErrorLog
{
    private readonly List<(string Id, Error Error)> _errors = new List<(string, Error)>();

    // Log an error and return a unique identifier for it
    public override string Log(Error error)
    {
        // Generate a new unique ID for the error
        string newId = Guid.NewGuid().ToString();

        // Store the error alongside its ID
        _errors.Add((newId, error));

        // Return the new ID
        return newId;
    }

    // Log an error with a provided GUID ID
    public override void Log(Guid id, Error error)
    {
        Log(error);
    }

    // Retrieve a single ErrorLogEntry by ID
    public override ErrorLogEntry GetError(string id)
    {
        // Find the error with the corresponding ID
        var entry = _errors.Find(e => e.Id.Equals(id));

        // Check if entry is not null
        if (entry.Error != null)
        {
            // Create an ErrorLogEntry, assume appropriate constructor
            return new ErrorLogEntry(this, id, entry.Error);
        }
        return null; // Return null if no error found with the given ID
    }

    // Retrieves a paginated collection of ErrorLogEntry objects
    public override int GetErrors(int pageIndex, int pageSize, ICollection<ErrorLogEntry> results)
    {
        int totalErrors = _errors.Count;
        int skip = pageIndex * pageSize;
        int take = Math.Min(pageSize, totalErrors - skip);

        for (int i = skip; i < skip + take; i++)
        {
            if (i < totalErrors)
            {
                var entry = _errors[i];
                // Add a new entry to results
                results.Add(new ErrorLogEntry(this, entry.Id, entry.Error)); // Use the stored ID and Error
            }
        }
        return totalErrors; // Total number of logged errors
    }
}

namespace Topshelf.Logging
{
    public class ElmahLogWriterFactory : LogWriterFactory
    {
        private readonly ElmahLogLevels _logLevels;

        public ElmahLogWriterFactory(ElmahLogLevels logLevels)
        {
            _logLevels = logLevels ?? throw new ArgumentNullException(nameof(logLevels));
        }

        public LogWriter Get(string name)
        {
            // Create an instance of SimpleErrorLog
            ErrorLog log = new SimpleErrorLog();
            return new ElmahLogWriter(log, _logLevels);
        }

        public void Shutdown()
        {
            // Handle any shutdown logic if required
        }

        public static void Use(ElmahLogLevels logLevels = null)
        {
            HostLogger.UseLogger(new ElmahHostLoggerConfigurator(logLevels ?? new ElmahLogLevels()));
        }

        [Serializable]
        public class ElmahHostLoggerConfigurator : HostLoggerConfigurator
        {
            private readonly ElmahLogLevels _logLevels;

            public ElmahHostLoggerConfigurator(ElmahLogLevels logLevels)
            {
                _logLevels = logLevels ?? throw new ArgumentNullException(nameof(logLevels));
            }

            public LogWriterFactory CreateLogWriterFactory()
            {
                return new ElmahLogWriterFactory(_logLevels);
            }
        }
    }
}
