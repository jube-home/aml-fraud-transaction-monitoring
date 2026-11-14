{{- define "jube.fullname" -}}
{{- printf "%s-jube" .Release.Name | trunc 50 | trimSuffix "-" -}}
{{- end -}}

{{- define "jube.labels" -}}
app.kubernetes.io/name: jube
app.kubernetes.io/instance: {{ .Release.Name }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
helm.sh/chart: {{ printf "%s-%s" .Chart.Name .Chart.Version }}
{{- end -}}

{{- define "jube.secretName" -}}
{{- if .Values.secrets.existingSecret -}}{{ .Values.secrets.existingSecret }}{{- else -}}{{ include "jube.fullname" . }}-secrets{{- end -}}
{{- end -}}

{{- define "jube.connection" -}}
{{- $p := .postgres -}}
{{- printf "Host=%s;Port=%v;Database=%s;Username=%s;Password=[@%s@];%s" .host .port $p.database .user .secret $p.poolOptions -}}
{{- end -}}
