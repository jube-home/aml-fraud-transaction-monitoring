---
layout: default
title: Activation Watcher
nav_order: 16
parent: Models
grand_parent: Configuration
---

# Activation Watcher

<!-- TODO(docs): this page was written for the Razor UI. The Map Entity Once switch, the Real Time mapping check-box and the menu location below are stale; the Vue page has Real time, Latest/All, Window, Fade, Trails and Heat controls. Owner to read the whole page. -->
The purpose of the Activation Watcher is to provide a real time ticker of Response Elevations, subject to the Activation
Rule having been set to send messages to the Activation Watcher.

The Activation Watcher is available in a main menu item:

<!-- TODO(screenshot): file=watcher-watcher-menu-location.png route=/watcher state="replaces the Razor-era WatcherMenuLocation.png; retake from the Vue page" -->
![Image](watcher-watcher-menu-location.png)

Navigate to the Watcher by clicking the menu item:

<!-- TODO(screenshot): file=watcher-watcher-page.png route=/watcher state="replaces the Razor-era WatcherPage.png; retake from the Vue page" -->
![Image](watcher-watcher-page.png)

Ensure that the Real Time mapping check-box is checked to allow plotting on a real-time basis, rather than only relying
on solely on Replay:

<!-- TODO(screenshot): file=watcher-enable-real-time-watcher.png route=/watcher state="replaces the Razor-era EnableRealTimeWatcher.png; retake from the Vue page" -->
![Image](watcher-enable-real-time-watcher.png)

The Map Entity Once switch exists to ensure that for the key (which would have been defined in the Activation Rule
definition) that a flag may only be plotted the once on a map for that key, so not to over-plot the map:

<!-- TODO(screenshot): file=watcher-enable-plot-once.png route=/watcher state="replaces the Razor-era EnablePlotOnce.png; retake from the Vue page" -->
![Image](watcher-enable-plot-once.png)

To configure an Activation Rule to dispatch real time messages to the Activation Watcher, navigate to the
Volume1DayUSDForIPOver100 for editing:

<!-- TODO(screenshot): file=watcher-navigate-to-volume1-day-u-s-d-for-i-p-over100.png route=/watcher state="replaces the Razor-era NavigateToVolume1DayUSDForIPOver100.png; retake from the Vue page" -->
![Image](watcher-navigate-to-volume1-day-u-s-d-for-i-p-over100.png)

Scroll down to the Response Elevation section:

<!-- TODO(screenshot): file=watcher-scroll-to-response-elevation.png route=/watcher state="replaces the Razor-era ScrollToResponseElevation.png; retake from the Vue page" -->
![Image](watcher-scroll-to-response-elevation.png)

Check the Send to Activation Watcher Switch to expose further options:

<!-- TODO(screenshot): file=watcher-expose-options-for-send-to-activation-watcher.png route=/watcher state="replaces the Razor-era ExposeOptionsForSendToActivationWatcher.png; retake from the Vue page" -->
![Image](watcher-expose-options-for-send-to-activation-watcher.png)

The fore colour and back colour is the colouring that will be included in the Activation Watcher for the entry tick. The
Activation Watcher has the ability to plot on a map, along with the background colour specified. The plotting of
activation rule matches on the map relies on a Latitude and Longitude having been specified in the Request XPath or the
Inline Script attributes, else it will plot [0,0] (just off Central Africa).

Update the model to Send To Activation Watcher alongside some tasteful colouring:

<!-- TODO(screenshot): file=watcher-set-activation-watcher-colour.png route=/watcher state="replaces the Razor-era SetActivationWatcherColour.png; retake from the Vue page" -->
![Image](watcher-set-activation-watcher-colour.png)

Scroll down and click update to create a new version of the Activation Rule:

<!-- TODO(screenshot): file=watcher-updated-activation-rule-with-activation-watcher.png route=/watcher state="replaces the Razor-era UpdatedActivationRuleWithActivationWatcher.png; retake from the Vue page" -->
![Image](watcher-updated-activation-rule-with-activation-watcher.png)

Navigate to the Watcher. Synchronise the model via Entity >> Synchronisation and repeat the HTTP POST to
endpoint [https://localhost:5001/api/invoke/EntityAnalysisModel/90c425fd-101a-420b-91d1-cb7a24a969cc](https://localhost:5001/api/invoke/EntityAnalysisModel/90c425fd-101a-420b-91d1-cb7a24a969cc)
for response as follows:

<!-- TODO(screenshot): file=watcher-ticked-out.png route=/watcher state="replaces the Razor-era TickedOut.png; retake from the Vue page" -->
![Image](watcher-ticked-out.png)

There will be nothing of note in the JSON response payload. Note that the pin has been plotted based on a latitude and
longitude sent to the watcher.

<!-- TODO(screenshot): file=watcher-wacther-response-payload.png route=/watcher state="replaces the Razor-era WactherResponsePayload.png; retake from the Vue page" -->
![Image](watcher-wacther-response-payload.png)

In this example the Latitude and Longitude is passed in the JSON transaction message:

<!-- TODO(screenshot): file=watcher-lat-long-in-request-payload.png route=/watcher state="replaces the Razor-era LatLongInRequestPayload.png; retake from the Vue page" -->
![Image](watcher-lat-long-in-request-payload.png)

With the values being specified as being of Data Type Latitude and Longitude in the Request XPath definition,
respectively:

<!-- TODO(screenshot): file=watcher-lat-in-request-x-path.png route=/watcher state="replaces the Razor-era LatInRequestXPath.png; retake from the Vue page" -->
![Image](watcher-lat-in-request-x-path.png)

The Activation Watcher will only tick through alerts as they happen when that page is connected.

The Replay functionality of the Activation Watcher - but only if the engines have been configured to store Activation
Watcher entries in the database - will query and replay these entries through the Activation Watcher as if they were
being sent in real-time.

Activation Watcher replay, is for a given date range, in ascending date order, for a maximum of 10000 records, as if
they were streaming in real-time. It should not be used to drive workflow, rather be considered a sample.

Set the date range to a day apart:

<!-- TODO(screenshot): file=watcher-activation-watcher-replay-date-range.png route=/watcher state="replaces the Razor-era ActivationWatcherReplayDateRange.png; retake from the Vue page" -->
![Image](watcher-activation-watcher-replay-date-range.png)

Before clicking the Replay button, it is recommended to turn off real time ticking, as this will eliminate confusion
between real time data and replay data:

<!-- TODO(screenshot): file=watcher-disable-real-time-watcher.png route=/watcher state="replaces the Razor-era DisableRealTimeWatcher.png; retake from the Vue page" -->
![Image](watcher-disable-real-time-watcher.png)

To replay, simply click the Replay button, lending a wide date range in this example:

<!-- TODO(screenshot): file=watcher-replay-map.png route=/watcher state="replaces the Razor-era ReplayMap.png; retake from the Vue page" -->
![Image](watcher-replay-map.png)

Keep in mind that the map is set to plot an Entity the once, only the grid and the chart will show real activation
frequencies:

<!-- TODO(screenshot): file=watcher-replay-ticks.png route=/watcher state="replaces the Razor-era ReplayTicks.png; retake from the Vue page" -->
![Image](watcher-replay-ticks.png)

There are several mechanisms that stop the Activation Watcher being flooded with data and causing the system to become
unresponsive. It follows that the Activation Watcher should be viewed to be a sample of activity for a period of time,
rather than a system of record or alert.

## The map, the stream and what leaves the server (Vue Watcher)

<!-- TODO(docs): owner to verify every statement in this section against the running app. -->

The Watcher page (route `/watcher`, permission 30 "View Watcher") draws activations on a map and in a feed. A stream of
events keeps it live and the Replay form reads stored rows.

### The map makes no remote requests by default

The map is drawn by a bundled library with a bundled world outline (country borders, Natural Earth public domain data
also used by the SVG charts). Out of the box the page requests no tiles, fonts, styles or sprites from any other host,
so the viewer's address and the coordinates being viewed never leave the deployment. Cluster counts are not labelled in
this mode because no font is bundled; clusters are sized by count instead.

A deployment that wants a street-level base map can opt in, knowing the privacy cost: the tile host sees the viewer's IP
address and the area being viewed, and with a hosted style the activations' neighbourhoods. Both steps are needed, on
purpose:

1. Build the single page application with `VITE_JUBE_MAP_STYLE_LIGHT` and `VITE_JUBE_MAP_STYLE_DARK` set to https style
   URLs (a value that is not https is ignored).
2. Add the tile host to `img-src` and `connect-src` of the `Content-Security-Policy` row in the `HttpResponseHeader`
   table. The stock policy names no remote host, so without this the browser blocks the request and the page falls back
   to the bundled outline.

<!-- TODO(docs): confirm how the owner wants deployers to set the build-time variables for the container image, and whether a runtime setting is preferred. -->
<!-- TODO(screenshot): file=watcher-map-offline-outline.png route=/watcher state="bundled outline with pins, a cluster and one impossible-travel trail" -->

The legacy Razor Watcher page (`/Watcher/Activation`) still loads OpenStreetMap tiles from the host named in its script;
the stock Content-Security-Policy now blocks that
host. <!-- TODO(docs): remove this paragraph when the Razor page is deleted. -->

### The stream

The live feed is a Server-Sent Events stream at `GET /api/Watcher/Stream`. It requires a signed-in user whose tenant
resolves and who holds permission 30, checked before any stream header is sent (403 otherwise). The stream is bound to
the caller's tenant on connect: the endpoint takes no tenant, group or id from the client, so there is nothing to tamper
with, and a message published for one tenant is only ever handed to that tenant's connections. Every 60 seconds the
permission and tenant are checked again, and a connection whose user lost the permission or now resolves to another
tenant is closed with a final `revoked` event. Sign-out, a revoked session, an expired absolute session lifetime and a
deactivated tenant close it on the next sweep of the session sweeper.

Limits: at most 10 open streams per user (429 beyond that; the page retries with a back-off from 5 to 60 seconds), at
most 256 queued messages per connection with the oldest dropped for a slow client, and messages over 64 KiB are dropped.
The page itself keeps at most 20000 undrawn activations.

What is streamed is the whole activation row: the key and key value (for example an account or card number from the
transaction), the latitude and longitude taken from the transaction, the rule name, the response elevation and its
content and colours. Treat permission 30 as access to those
values. <!-- TODO(docs): owner to confirm whether key values should be masked for users without a case permission. -->
