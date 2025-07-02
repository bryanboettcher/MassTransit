namespace MassTransit
{
    using System;


    [Serializable]
    public class SagaConcurrencyException :
        ConcurrencyException
    {
        public SagaConcurrencyException(string message, Type sagaType, Guid correlationId)
            : base(message, sagaType, correlationId)
        {
        }

        public SagaConcurrencyException(string message, Type sagaType, Guid correlationId, Exception innerException)
            : base(message, sagaType, correlationId, innerException)
        {
        }

        public SagaConcurrencyException()
        {
        }
    }
}
