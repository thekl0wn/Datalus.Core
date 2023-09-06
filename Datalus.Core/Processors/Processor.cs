namespace Datalus.Core
{
    public abstract class Processor<T> : IProcessor<T> where T : Component
    {
        protected Processor()
        {
            // set runtime
            _runtime_id = RuntimeController.NextID();

            // register
            ProcessorController.Register(this);
        }

        /// <summary>
        /// Runtime ID. Set at runtime as a unique identifier across the system.
        /// </summary>
        public int RuntimeID { get => _runtime_id; }
        private int _runtime_id;
    }
}
