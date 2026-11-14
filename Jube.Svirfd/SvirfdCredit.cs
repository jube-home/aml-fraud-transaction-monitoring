/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */
namespace Jube.Svirfd
{
    public static class SvirfdCredit
    {
        public const string Line =
            "Data: svirfd.com, built from RIR delegated statistics, RouteViews, RIPE NCC RIS, open traceroutes, " +
            "Wikidata (CC0) and operator-published lists. Credit svirfd.com where this data is shown.";

        public const string Credit = "svirfd.com";
        public const string SiteUrl = "https://svirfd.com";
        public const string SourcesUrl = "https://svirfd.com/sources";
        public const string LicenceUrl = "https://svirfd.com/licence";

        public const string RouteViews =
            "This service utilizes data provided by RouteViews (www.routeviews.org). Use of this data is subject " +
            "to the CC BY 4.0 license. DOI 10.7264/1y7v-2d90.";

        public const string RouteViewsUrl = "https://www.routeviews.org";

        public const string Ris =
            "Routing data from the RIPE NCC Routing Information Service (RIS), a RIPE NCC project that collects " +
            "and stores BGP routing data from locations around the globe.";

        public const string RisUrl = "https://www.ripe.net/ris";
    }
}
