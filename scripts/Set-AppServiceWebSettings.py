#!/usr/bin/env python3
"""Merge App Service application settings via Kudu using a publish profile.

Reads AZURE_WEBAPP_PUBLISH_PROFILE from the environment. Never prints the
profile, username, or password. Used by deploy.yml to turn on
WEBSITE_RUN_FROM_PACKAGE and the platform warmup path in the same job as
the zip push, so the first enablement does not recycle production ahead
of a package being present.
"""

from __future__ import annotations

import base64
import json
import os
import sys
import urllib.error
import urllib.request
import xml.etree.ElementTree as ET

SETTINGS = {
    "WEBSITE_RUN_FROM_PACKAGE": "1",
    "WEBSITE_WARMUP_PATH": "/warmup",
    "WEBSITE_WARMUP_STATUSES": "200",
}


def parse_msdeploy_profile(xml_text: str) -> tuple[str, str, str]:
    root = ET.fromstring(xml_text)
    profiles = root.findall(".//publishProfile")
    if root.tag == "publishProfile":
        profiles = [root]
    for profile in profiles:
        if profile.get("publishMethod") != "MSDeploy":
            continue
        user = profile.get("userName") or ""
        password = profile.get("userPWD") or ""
        publish_url = profile.get("publishUrl") or ""
        if user and password and publish_url:
            host = publish_url.split(":")[0]
            return user, password, host
    raise SystemExit("MSDeploy publish profile with userName/userPWD/publishUrl was not found.")


def main() -> int:
    xml_text = os.environ.get("AZURE_WEBAPP_PUBLISH_PROFILE", "").strip()
    if not xml_text:
        print("::error::AZURE_WEBAPP_PUBLISH_PROFILE is empty.", file=sys.stderr)
        return 1

    user, password, host = parse_msdeploy_profile(xml_text)
    print(f"::add-mask::{user}")
    print(f"::add-mask::{password}")

    token = base64.b64encode(f"{user}:{password}".encode("utf-8")).decode("ascii")
    url = f"https://{host}/api/settings"
    body = json.dumps(SETTINGS).encode("utf-8")
    request = urllib.request.Request(
        url,
        data=body,
        method="POST",
        headers={
            "Authorization": f"Basic {token}",
            "Content-Type": "application/json",
        },
    )

    try:
        with urllib.request.urlopen(request, timeout=60) as response:
            status = response.status
    except urllib.error.HTTPError as exc:
        print(
            f"::error::Kudu /api/settings returned HTTP {exc.code}.",
            file=sys.stderr,
        )
        return 1
    except urllib.error.URLError as exc:
        print(f"::error::Kudu /api/settings failed: {exc.reason}", file=sys.stderr)
        return 1

    print(
        f"Merged {', '.join(sorted(SETTINGS))} via Kudu (HTTP {status}). "
        "Keys only — values are not logged."
    )
    return 0


def _self_test() -> int:
    sample = """<publishData>
  <publishProfile publishMethod="FTP" userName="ftp-user" userPWD="ftp-secret" publishUrl="ftp.example.com" />
  <publishProfile publishMethod="MSDeploy" userName="$site" userPWD="s3cret" publishUrl="site.scm.azurewebsites.net:443" />
</publishData>"""
    user, password, host = parse_msdeploy_profile(sample)
    assert user == "$site", user
    assert password == "s3cret", password
    assert host == "site.scm.azurewebsites.net", host
    print("Set-AppServiceWebSettings self-test passed.")
    return 0


if __name__ == "__main__":
    if "--self-test" in sys.argv:
        raise SystemExit(_self_test())
    raise SystemExit(main())
