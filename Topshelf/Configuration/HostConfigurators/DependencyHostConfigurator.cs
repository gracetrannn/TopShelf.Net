﻿// Copyright 2007-2013 Chris Patterson, Dru Sellers, Travis Smith, et. al.
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
namespace Topshelf.HostConfigurators
{
    using System;
    using System.Collections.Generic;
    using Builders;
    using Configurators;


    /// <summary>
    /// Adds a dependency to the InstallBuilder (ignored otherwise)
    /// </summary>
    public class DependencyHostConfigurator :
        HostBuilderConfigurator
    {
        public DependencyHostConfigurator(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            Name = name;
        }

        public string Name { get; private set; }

        public IEnumerable<ValidateResult> Validate()
        {
            if (string.IsNullOrEmpty(Name))
                yield return this.Failure("Dependency", "must not be null");
        }

        public HostBuilder Configure(HostBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.Match<InstallBuilder>(x => x.AddDependency(Name));

            return builder;
        }
    }
}