# Datalus.Core
Core functionality for Datalus ECS

## The Major Terms
The core is busted out into a few types of classes to be utilized throughout.

### Controller
Controllers are specialty static classes.

### Components
Components are the bread-and-butter of the Datalus.Core system.

### Entities
Entities are basically component containers with a unique ID.

### Processors
Processors ("systems" in typical ECS) process the components of the same type.

### Managers
Managers house and manage "types" of entities.
