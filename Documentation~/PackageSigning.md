# Package Signing for Unity 6+

Starting with Unity 6.3, the Package Manager checks for digital signatures on all tarball packages. This document explains how to sign the Unity MCP Sharp package for distribution.

## Why Sign Packages?

Package signatures:
- Verify the package source (which organization created it)
- Ensure the package hasn't been tampered with
- Provide trust indicators in Unity's Package Manager

Without signing, Unity 6+ users will see a "Warning" status in Package Manager, indicating no signature is present.

## Signature Status Levels

| Status | Meaning | User Experience |
|--------|---------|-----------------|
| **Full** | Signed by Unity or user's organization | Safe to use |
| **Limited** | Signed by public/unfamiliar organization | Verify source before use |
| **Error** | Invalid signature | Possible tampering |
| **Warning** | No signature | Request signed version |

## Local Package Signing

### Prerequisites

1. **Unity 6.3+ Editor** installed via Unity Hub
2. **Unity Cloud Organization** - [Create one here](https://cloud.unity.com/account/my-organizations)
3. **Unity Account** with organization membership

### Setup

1. Copy `.env.example` to `.env`:
   ```bash
   cp .env.example .env
   ```

2. Fill in your credentials in `.env`:
   ```bash
   UNITY_EMAIL=your-unity-account@email.com
   UNITY_PASSWORD=your-unity-password
   UNITY_ORG_ID=your-organization-id
   ```

   Get your Organization ID from: https://cloud.unity.com/account/my-organizations

3. (Optional) Set custom Unity path if not auto-detected:
   ```bash
   UNITY_PATH=/Applications/Unity/Hub/Editor/6000.3.0f1/Unity.app/Contents/MacOS/Unity
   ```

### Sign the Package

Run the signing script:

```bash
./Scripts~/sign-package.sh
```

Or with a specific version:

```bash
./Scripts~/sign-package.sh 0.6.0
```

This creates a signed `.tgz` file: `unity-mcp-sharp-{version}.tgz`

### Manual Signing

You can also sign manually using Unity CLI:

```bash
# macOS
/Applications/Unity/Hub/Editor/6000.3.0f1/Unity.app/Contents/MacOS/Unity \
    -batchmode -quit \
    -username "your@email.com" \
    -password "your-password" \
    -upmPack "." "output.tgz" \
    -cloudOrganization "your-org-id" \
    -logfile -

# Windows
"C:\Program Files\Unity\Hub\Editor\6000.3.0f1\Editor\Unity.exe" ^
    -batchmode -quit ^
    -username "your@email.com" ^
    -password "your-password" ^
    -upmPack "." "output.tgz" ^
    -cloudOrganization "your-org-id" ^
    -logfile -
```

## CI/CD Package Signing (GitHub Actions)

The release workflow supports automated package signing when properly configured.

### Required Secrets

Add these secrets in GitHub repository settings:

| Secret | Description |
|--------|-------------|
| `UNITY_EMAIL` | Unity account email |
| `UNITY_PASSWORD` | Unity account password |
| `UNITY_ORG_ID` | Unity Cloud Organization ID |
| `UNITY_LICENSE` | Unity license file content (for GameCI) |

### Required Variables

Add this variable in GitHub repository settings:

| Variable | Value | Description |
|----------|-------|-------------|
| `ENABLE_PACKAGE_SIGNING` | `true` | Enables the signing job |

### Getting Unity License for CI

1. Run locally:
   ```bash
   unity-editor -batchmode -createManualActivationFile
   ```

2. Upload the `.alf` file to [Unity License Portal](https://license.unity3d.com/manual)

3. Download the `.ulf` license file

4. Add the contents of `.ulf` as the `UNITY_LICENSE` secret

### How It Works

1. When a version tag is pushed (`v*`), the workflow triggers
2. If `ENABLE_PACKAGE_SIGNING=true`, the signing job runs:
   - Installs Unity 6.3 via GameCI
   - Signs the package with Unity CLI
   - Uploads signed `.tgz` as artifact
3. The release job creates a GitHub Release:
   - Attaches the signed `.tgz` if available
   - Includes installation instructions

### Without Signing Configured

If signing is not configured:
- The signing job is skipped
- Releases are still created normally
- Users can install via OpenUPM (with signature warnings in Unity 6+)

## Distributing Signed Packages

### Option 1: GitHub Release Download

Users can download the signed `.tgz` from GitHub Releases and install via:
- Unity Package Manager > "Install package from tarball"
- Or drag-drop the `.tgz` into Unity

### Option 2: OpenUPM (Recommended)

OpenUPM handles package distribution. Note that packages from OpenUPM may still show "Limited" signature status since they come from a third-party registry.

## Troubleshooting

### "Unity Editor not found"

Set `UNITY_PATH` in your `.env` file to point to your Unity 6.3+ installation.

### "Invalid credentials"

1. Verify your Unity account email and password
2. Check you're a member of the organization
3. Try logging into Unity Hub to verify credentials work

### "Organization not found"

1. Go to https://cloud.unity.com/account/my-organizations
2. Copy the Organization ID (not the name)
3. Ensure you have the "Package Publisher" role in the organization

### Signed package still shows "Warning"

The signature might be for a different organization than expected. Check:
1. You're signed into Unity with an account in that organization
2. The organization ID matches what was used for signing

## References

- [Unity Package Signatures Documentation](https://docs.unity3d.com/6000.3/Documentation/Manual/upm-signature.html)
- [Export and Sign Packages](https://docs.unity3d.com/6000.3/Documentation/Manual/cus-export.html)
- [Unity Cloud Organizations](https://cloud.unity.com/account/my-organizations)
- [GameCI Unity Builder](https://game.ci/docs/github/builder)
