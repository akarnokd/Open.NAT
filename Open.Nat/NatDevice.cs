//
// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//   Ben Motmans <ben.motmans@gmail.com>
//   Lucas Ontivero lucasontivero@gmail.com
//
// Copyright (C) 2006 Alan McGovern
// Copyright (C) 2007 Ben Motmans
// Copyright (C) 2014 Lucas Ontivero
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Open.Nat
{
    /// <summary>
    /// Represents a NAT device and provides access to the operation set that allows
    /// open (forward) ports, close ports and get the externa (visible) IP address.
    /// </summary>
	public abstract class NatDevice
    {
        private readonly List<Mapping> _openMapping = new List<Mapping>();
	    protected DateTime LastSeen { get; private set; }

        internal void Touch()
        {
            LastSeen = DateTime.Now;
        }

        /// <summary>
        /// Creates the port map asynchronous.
        /// </summary>
        /// <param name="mapping">The <see cref="Mapping">Mapping</see> entry.</param>
        /// <example>
        /// device.CreatePortMapAsync(new Mapping(Protocol.Tcp, 1700, 1600));
        /// </example>
        /// <exception cref="MappingException">MappingException</exception>
        public abstract Task CreatePortMapAsync(Mapping mapping);
        /// <summary>
        /// Deletes a mapped port asynchronous.
        /// </summary>
        /// <param name="mapping">The <see cref="Mapping">Mapping</see> entry.</param>
        /// <example>
        /// device.DeletePortMapAsync(new Mapping(Protocol.Tcp, 1700, 1600));
        /// </example>
        /// <exception cref="MappingException">MappingException</exception>
        public abstract Task DeletePortMapAsync(Mapping mapping);

        /// <summary>
        /// Gets all mappings asynchronous.
        /// </summary>
        /// <returns>
        /// The list of all forwarded ports
        /// </returns>
        /// <example>
        /// var mappings = await device.GetAllMappingsAsync();
        /// foreach(var mapping in mappings)
        /// {
        ///     Console.WriteLine(mapping)
        /// }
        /// </example>
        /// <exception cref="MappingException">MappingException</exception>
        public abstract Task<IEnumerable<Mapping>> GetAllMappingsAsync();
        /// <summary>
        /// Gets the external (visible) IP address asynchronous. This is the NAT device IP address
        /// </summary>
        /// <returns>
        /// The public IP addrees
        /// </returns>
        /// <example>
        /// Console.WriteLine("My public IP is: {0}", await device.GetExternalIPAsync());
        /// </example>
        /// <exception cref="MappingException">MappingException</exception>
        public abstract Task<IPAddress> GetExternalIPAsync();
        /// <summary>
        /// Gets the specified mapping asynchronous.
        /// </summary>
        /// <param name="protocol">The protocol.</param>
        /// <param name="port">The port.</param>
        /// <returns>
        /// The matching mapping
        /// </returns>
        public abstract Task<Mapping> GetSpecificMappingAsync(Protocol protocol, int port);

        protected void RegisterMapping(Mapping mapping)
        {
            _openMapping.Add(mapping);    
        }

        protected void UnregisterMapping(Mapping mapping)
        {
            _openMapping.Remove(mapping);
        }

        internal void ReleaseAll()
        {
            var mapCount = _openMapping.Count;
            NatUtility.TraceSource.LogInfo("{0} ports to close", mapCount);
            for (var i = 0; i < mapCount; i++)
            {
                var mapping = _openMapping[i];
                var log = string.Format("{0} {1} --> {2}:{3} ({4}) port",
                    mapping.Protocol == Protocol.Udp ? "Tcp" : "Udp",
                    mapping.PublicPort,
                    mapping.PrivateIP,
                    mapping.PrivatePort,
                    mapping.Description);

                try
                {
                    DeletePortMapAsync(mapping);
                    NatUtility.TraceSource.LogInfo( log + " successfully closed"); 
                }
                catch(Exception e)
                {
                    NatUtility.TraceSource.LogError( log + " couldn't be close");
                    NatUtility.TraceSource.LogError(e.ToString());
                }
            }
            _openMapping.Clear();
        }
	}
}