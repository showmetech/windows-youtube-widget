# Existing Microsoft Store publisher

Use the existing publisher **Showmetech**, company **SHOWMETECH LTDA**, bruno@showmetech.com.br. On 2026-11-07 email/employment were verified, but business documents were in review. Preserve this account; do not register another company to bypass review.

After Microsoft releases the account:

1. Reserve the app under that publisher. Copy the exact Product ID, Package/Identity/Name and Package/Identity/Publisher from Product management / Product identity into packaging/store-config.json. Never invent these values.
2. Submit the initial MSIX bundle through Partner Center. Complete the listing, age rating, markets, privacy policy, real screenshots and testing notes accurately. This is an independent video player, not an official Google/YouTube/Microsoft product.
3. Request approval for unvirtualizedResources. Chrome/Edge launch the native host outside the MSIX context; the app and host must see the same app-owned manifest, AppData state and HKCU NativeMessagingHosts entries. Windows 11 declarations narrow the shared paths; Windows 10 uses older declarations. Set unvirtualizedResourcesApproved only after actual Microsoft approval.
4. Run the real Store package on a test account: startup, tray-only behavior, native messaging, handoff, fixed-runtime playback, update and uninstall. Run Windows App Certification Kit. Development artifacts prove schema/build preparation, not Store installation or certification.
5. Once the first product is live, set firstPublicationComplete and enabled to true. Reuse an authorized Entra application where possible. Creating credentials or granting a new Manager role requires user authorization at that step.
6. Store AZURE_AD_TENANT_ID, SELLER_ID, AZURE_AD_APPLICATION_CLIENT_ID and AZURE_AD_APPLICATION_SECRET as GitHub Actions secrets, never repository files. Validate the authenticated publisher and role by a read operation first.
7. The release workflow submits the three-architecture bundle with the official Microsoft Store CLI. submitted means certification is pending. Verify the actual live product version separately before calling it published.

Never commit private keys, identity documents, address/telephone details or browser sessions.

References:

- https://learn.microsoft.com/windows/apps/publish/msstore-dev-cli/github-actions
- https://learn.microsoft.com/windows/apps/publish/publish-your-app/msix/app-package-requirements
- https://learn.microsoft.com/windows/msix/desktop/flexible-virtualization
- https://learn.microsoft.com/uwp/api/windows.applicationmodel.startuptask
