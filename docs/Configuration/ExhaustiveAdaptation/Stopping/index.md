---
layout: default
title: Stopping Training
nav_order: 6
parent: Exhaustive Adaptation
grand_parent: Configuration
---

Exhaustive model training can take a very long time, and it is computationally expensive. To stop an Exhaustive training
instance identify the Stop button:

<!-- TODO(screenshot): file=model-exhaustive-location-of-stop-button.png route=/models state="Stopping: replace the Razor-era image LocationOfStopButton.png with the Vue Exhaustive page" -->
![LocationOfStopButton.png](model-exhaustive-location-of-stop-button.png)

On clicking the Stop button the training status is set to Stopping:

<!-- TODO(screenshot): file=model-exhaustive-stopping.png route=/models state="Stopping: replace the Razor-era image Stopping.png with the Vue Exhaustive page" -->
![Stopping.png](model-exhaustive-stopping.png)

The Exhaustive training thread updates the status to signal progress. Before each update of a status by the Exhaustive
training thread,
the same status is checked to see if it is in a Stopping state. In the event that the status is in a Stopping state, the
state is finalised to Stop instead, and the Exhaustive training thread gracefully returned uncompleted. It is worth
noting
that the task in hand will be completed and the thread is not interrupted or aborted, hence it can still be a moderately
time-consuming process to reach a consistent stopping point. The thread is immediately available for the next Exhaustive
training instance.

Once the stopping point has been reached, the status is finalised to Stopped:

<!-- TODO(screenshot): file=model-exhaustive-stopped.png route=/models state="Stopping: replace the Razor-era image Stopped.png with the Vue Exhaustive page" -->
![Stopped.png](model-exhaustive-stopped.png)



