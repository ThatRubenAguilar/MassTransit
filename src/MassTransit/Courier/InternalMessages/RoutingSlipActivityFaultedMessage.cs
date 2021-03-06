// Copyright 2007-2015 Chris Patterson, Dru Sellers, Travis Smith, et. al.
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
namespace MassTransit.Courier.InternalMessages
{
    using System;
    using System.Collections.Generic;
    using Contracts;


    class RoutingSlipActivityFaultedMessage :
        RoutingSlipActivityFaulted
    {
        public RoutingSlipActivityFaultedMessage(HostInfo host, Guid trackingNumber, string activityName, Guid activityTrackingNumber,
            DateTime timestamp, TimeSpan duration, ExceptionInfo exceptionInfo, IDictionary<string, object> variables,
            IDictionary<string, object> arguments)
        {
            Host = host;
            TrackingNumber = trackingNumber;
            Timestamp = timestamp;
            Duration = duration;
            ActivityTrackingNumber = activityTrackingNumber;
            ActivityName = activityName;
            Variables = variables;
            Arguments = arguments;
            ExceptionInfo = exceptionInfo;
        }

        public Guid TrackingNumber { get; private set; }
        public DateTime Timestamp { get; private set; }
        public TimeSpan Duration { get; private set; }
        public string ActivityName { get; private set; }
        public HostInfo Host { get; private set; }
        public Guid ActivityTrackingNumber { get; private set; }
        public ExceptionInfo ExceptionInfo { get; private set; }
        public IDictionary<string, object> Arguments { get; private set; }
        public IDictionary<string, object> Variables { get; private set; }
    }
}