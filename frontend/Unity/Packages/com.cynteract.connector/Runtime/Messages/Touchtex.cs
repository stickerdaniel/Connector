#nullable enable

using System;

namespace Connector.Messages
{
    [Serializable]
    public class Touchtex
    {
        static Touchtex() => Factory.RegisterMessageType("touchtex", typeof(Touchtex));

        public byte mainVibration;
        public byte[]? fingerVibrations;
        public TouchTexPart[]? touchTexBoards;
        public byte frontPressure, backPressure;
    }
    [Serializable]
    public class TouchTexPart
    {
        public byte[]? vibrations;
        public byte heat;
    }
}
