# SafetyRatingPlugin
Plugin to calculate player safety ratings based on their collitions.
Each player starts with a base value, each time there is a collision a certain amount
will be deducted based on whether they collided with the environment, traffic or other player.
Only a certain window of time of events will be used - this can be configured.

## Configuration
Enable the plugin in `extra_cfg.yml`
```yaml
EnablePlugins:
- SafetyRatingPlugin
```

Example configuration (add to bottom of `extra_cfg.yml`)  
```yaml
---
!SafetyRatingConfiguration
# The starting value for Safety Rating
BaseValue: 100
# Multiplier for collisions with traffic
TrafficMultiplier: 2
# Multiplier for collisions with other players
PlayerMultiplier: 3
# Multiplier for collisions with environment
EnvironmentMultiplier: 1
# What is the window duration for collision events in minutes
Duration: 10
```