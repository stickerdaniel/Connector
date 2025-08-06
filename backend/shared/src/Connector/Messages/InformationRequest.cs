using System;

namespace Connector.Messages
{
    [Serializable]
    public class InformationRequest
    {
        static InformationRequest() => Factory.RegisterMessageType("informationRequest", typeof(InformationRequest));
    }
}