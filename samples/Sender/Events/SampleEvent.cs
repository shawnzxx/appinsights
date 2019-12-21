namespace Sender.Events
{
    public class SampleEvent
    {
        public string Info { get; }

        public SampleEvent(string eventInfo)
        {
            Info = eventInfo;
        }
    }
}
