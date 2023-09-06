using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datalus.Core
{
    public abstract class Manager : IManager
    {
        protected Manager()
        {
            // setup runtime
            _runtime_id = RuntimeController.NextID();

            // create entity list
            _entities = new List<Entity>();

            // register the manager
            ManagerController.Register(this);
        }

        /// <summary>
        /// Number of entities managed by the manager.
        /// </summary>
        public int Count { get => _entities.Count; }

        /// <summary>
        /// List of entities managed by manager.
        /// </summary>
        public IReadOnlyList<IEntity> Entities { get => _entities; }
        private List<Entity> _entities;

        /// <summary>
        /// Runtime ID. Set at runtime as a unique identifier across the system.
        /// </summary>
        public int RuntimeID { get => _runtime_id; }
        private int _runtime_id;

        /// <summary>
        /// Create new instance of an entity.
        /// </summary>
        /// <returns>reference to the newly created entity</returns>
        public IEntity Create()
        {
            // create new entity
            var entity = new Entity();

            // register
            Register(entity);

            // return entity
            return entity;
        }

        /// <summary>
        /// Create new instance of a specific entity.
        /// </summary>
        /// <param name="id">ID of the entity to instantiate</param>
        /// <returns>instatiated entity (or null if id not fould)</returns>
        public IEntity Get(int id)
        {
            // create entity
            var entity = new Entity();

            // load it
            if (!entity.LoadData(id)) return null;

            // register
            Register(entity);

            // return entity
            return entity;
        }

        /// <summary>
        /// Internally registers the entity to the manager.
        /// </summary>
        /// <param name="entity">entity to register</param>
        private void Register(Entity entity)
        {
            // add
            _entities.Add(entity);

            // register centrally
            EntityController.Register(entity);
        }
    }
}
