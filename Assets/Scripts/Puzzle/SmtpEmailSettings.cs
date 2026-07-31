using UnityEngine;

[CreateAssetMenu(fileName = "SmtpEmailSettings", menuName = "SGH/SMTP Email Settings")]
public class SmtpEmailSettings : ScriptableObject
{
    [Header("Email backend")]
    [Tooltip("The HTTPS URL of the deployed backend endpoint, ending in /api/gameplay-csv.")]
    public string emailApiUrl = "https://YOUR-BACKEND.example.com/api/gameplay-csv";

    [Tooltip("Maximum number of seconds Unity waits for the backend to accept the CSV.")]
    [Range(1, 60)]
    public int requestTimeoutSeconds = 20;

    [Tooltip("Disable this to keep CSV creation on while preventing upload to the email backend.")]
    public bool enableSending = true;

    public bool IsConfigured(out string reason)
    {
        if (!enableSending)
        {
            reason = "Email sending is disabled in SmtpEmailSettings.";
            return false;
        }

        if (!System.Uri.TryCreate(emailApiUrl, System.UriKind.Absolute, out System.Uri endpoint))
        {
            reason = "SmtpEmailSettings needs a valid email backend URL.";
            return false;
        }

        bool isHttps = endpoint.Scheme == System.Uri.UriSchemeHttps;
#if UNITY_EDITOR
        bool isLocalEditorEndpoint = endpoint.Scheme == System.Uri.UriSchemeHttp && endpoint.IsLoopback;
#else
        bool isLocalEditorEndpoint = false;
#endif
        if (!isHttps && !isLocalEditorEndpoint)
        {
            reason = "Email backend URL must use HTTPS (except a localhost URL while running in the Unity Editor).";
            return false;
        }

        reason = null;
        return true;
    }
}
