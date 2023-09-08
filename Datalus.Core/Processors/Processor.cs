using System.Collections.Generic;

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

        public IReadOnlyList<IComponent> Components { get => _components; }
        private List<Component> _components = new List<Component>();

        /// <summary>
        /// Runtime ID. Set at runtime as a unique identifier across the system.
        /// </summary>
        public int RuntimeID { get => _runtime_id; }
        private int _runtime_id;

        /// <summary>
        /// Update all components registered with the processor
        /// </summary>
        public void Update()
        {
            // loop through the components
            foreach(var component in _components)
            {
                // update each component
                component.Update();
            }
        }
    }
}
