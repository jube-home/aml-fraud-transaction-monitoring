---
layout: default
title: Stopping Training
nav_order: 6
parent: Exhaustive Adaptation
grand_parent: Configuration
---

# Stopping Training

Exhaustive model training can take a very long time, and it is computationally expensive. It follows that it must be possible to bring an Exhaustive training instance to an end before it has run its course, without losing what it has learned thus far.

## Where it lives

Navigate through the menu to Models, open the model, then the Exhaustive Adaptation hub card, and open the Exhaustive Adaptation. Stop is presented in the status band at the head of the record, beside Refresh, for as long as training is awaiting the server or running, and is not offered once training has finished, is stopping or has stopped. The band is described in [Training Configuration](../TrainingConfiguration/index.html).

## Behaviour

On clicking Stop the training status is set to Stopping, and the band reflects this without the page being reloaded: the dot beside the status turns amber, the progress bar remains at the stage reached, and Stop is withdrawn, there being no confirmation sought beforehand and no means of resuming. While the request is being made Stop cannot be pressed a second time. Should the request fail, the page states The adaptation could not be stopped. beneath the band and the status is left as it was.

By way of example, FraudScore having reached Supervised learning with 320 models trained, 300 of them since the best, Stop is pressed. The band reads Stopping until the trial in hand has completed, and then Stopped with a red dot; Models trained no longer rises, the six promoted models remain in Which model is live with the best of them active, and every results section continues to be available.

The Exhaustive training thread updates the status to signal progress. Before each update of a status by the Exhaustive training thread, and before each trial, the same status is checked to see if it is in a Stopping state. In the event that the status is in a Stopping state, the state is finalised to Stopped instead, and the Exhaustive training thread gracefully returned uncompleted. It is worth noting that the task in hand will be completed and the thread is not interrupted or aborted, hence it can still be a moderately time-consuming process to reach a consistent stopping point. The thread is immediately available for the next Exhaustive training instance.

Once the stopping point has been reached, the status is finalised to Stopped. Stopping does not remove anything already recorded, such that an Exhaustive Adaptation stopped during Supervised learning retains its results and the models promoted before the stop.

| Status   | Description                                                                                             |
|----------|---------------------------------------------------------------------------------------------------------|
| Stopping | A stop has been requested, and the training thread has yet to reach a consistent stopping point.        |
| Stopped  | The training thread has returned, and is available for the next Exhaustive training instance.           |

An Exhaustive Adaptation that is stopped while still Awaiting server is held by no training thread, and is therefore set to Stopped at once, without passing through Stopping.

## Permissions and security

Stopping training requires permission 16, Read Write Exhaustive, and a user without it is told You do not have permission to stop this adaptation. A stop is requested by the Guid of the Exhaustive Adaptation, by `PUT /api/ExhaustiveSearchInstance/stop/{guid}`, and a request for a Guid that is not that of an Exhaustive Adaptation of the tenant of the user does nothing.
