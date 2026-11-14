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
using Jube.Service.Repository.CaseFile.Models;

namespace Jube.Service.Repository.CaseFile
{
    public sealed class CaseFileTypePolicy
    {
        public const string ExtensionsSetting = "CaseFileAllowedExtensions";
        public const string ContentTypesSetting = "CaseFileAllowedContentTypes";

        public const string DefaultExtensions =
            "pdf,doc,docx,xls,xlsx,ppt,pptx,odt,ods,odp,rtf,txt,csv,tsv,json,xml,md,log,png,jpg,jpeg,gif,webp,bmp," +
            "tif,tiff,heic,zip,7z,gz,tgz,tar,eml,msg,mbox,mp3,wav,mp4,mov";

        public const string DefaultContentTypes =
            "application/pdf,image/png,image/jpeg,image/gif,image/webp,image/bmp,image/tiff,application/zip," +
            "application/x-ole-storage,application/x-7z-compressed,application/gzip,application/x-tar," +
            "application/rtf,audio/mpeg,audio/wav,video/mp4,text/plain";

        private readonly HashSet<string>? contentTypes;
        private readonly HashSet<string>? extensions;

        private CaseFileTypePolicy(HashSet<string>? extensions, HashSet<string>? contentTypes)
        {
            this.extensions = extensions;
            this.contentTypes = contentTypes;
        }

        public static CaseFileTypePolicy FromEnvironment(DynamicEnvironment.DynamicEnvironment? environment)
        {
            return new CaseFileTypePolicy(
                Parse(environment?.AppSettings(ExtensionsSetting), DefaultExtensions, true),
                Parse(environment?.AppSettings(ContentTypesSetting), DefaultContentTypes, false));
        }

        public CaseFileTypeVerdict Evaluate(string fileName, byte[] content)
        {
            if (extensions != null)
            {
                var extension = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();

                if (extension.Length == 0 || !extensions.Contains(extension))
                {
                    return CaseFileTypeVerdict.ExtensionNotAllowed;
                }
            }

            if (contentTypes != null && !contentTypes.Contains(CaseFileContentSniffer.Sniff(content)))
            {
                return CaseFileTypeVerdict.ContentNotAllowed;
            }

            return CaseFileTypeVerdict.Accepted;
        }

        private static HashSet<string>? Parse(string? configured, string fallback, bool extension)
        {
            var text = string.IsNullOrWhiteSpace(configured) ? fallback : configured;

            var items = text.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries |
                                                      StringSplitOptions.TrimEntries)
                .Select(s => extension ? s.TrimStart('.') : s)
                .Select(s => s.ToLowerInvariant())
                .ToHashSet(StringComparer.Ordinal);

            return items.Contains("*") ? null : items;
        }
    }
}
