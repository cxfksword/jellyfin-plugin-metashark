#!/usr/bin/env python3
import hashlib
import json
import sys
import re
import os
import subprocess
from datetime import datetime
from urllib.request import urlopen
from urllib.error import HTTPError


def generate_manifest():
    return    [{
        "guid": "9a19103f-16f7-4668-be54-9a1e7a4f7556",
        "name": "MetaShark",
        "description": "jellyfin电影元数据插件，影片信息只要从豆瓣获取，并由TMDB补充缺失的剧集数据。",
        "overview": "jellyfin电影元数据插件",
        "owner": "cxfksword",
        "category": "Metadata",
        "imageUrl": "https://jellyfin-plugin-release.pages.dev/metashark/logo.png",
        "versions": []
    }]

def generate_version(filepath, version, changelog):
    return {
        'version': f"{version}.0",
        'changelog': changelog,
        'targetAbi': '10.8.0.0',
        'sourceUrl': f'https://jellyfin-plugin-release.pages.dev/metashark/metashark_{version}.0.zip',
        'checksum': md5sum(filepath),
        'timestamp': datetime.now().strftime('%Y-%m-%dT%H:%M:%S')
    }

def md5sum(filename):
    with open(filename, 'rb') as f:
        return hashlib.md5(f.read()).hexdigest()


def main():
    filename = sys.argv[1]
    tag = sys.argv[2]
    version = tag.lstrip('v')
    filepath = os.path.join(os.getcwd(), filename)
    result = subprocess.run(['git', 'tag','-l','--format=%(contents)', tag, '-l'], stdout=subprocess.PIPE)
    changelog = result.stdout.decode('utf-8').strip()

    # 解析旧 manifest
    try:
        with urlopen('https://raw.githubusercontent.com/cxfksword/jellyfin-release/master/metashark/manifest.json') as f:
            manifest = json.load(f)
    except HTTPError as err:
        if err.code == 404:
            manifest = generate_manifest()
        else:
            raise

    # 追加新版本/覆盖旧版本
    manifest[0]['versions'] = list(filter(lambda x: x['version'] == version, manifest[0]['versions']))
    manifest[0]['versions'].insert(0, generate_version(filepath, version, changelog))

    with open('manifest.json', 'w') as f:
        json.dump(manifest, f, indent=2)

    # # 国内加速
    # with open('manifest_cn.json', 'w') as f:
    #     manifest_cn = json.dumps(manifest, indent=2)
    #     manifest_cn = re.sub('https://github.com/cxfksword/jellyfin-plugin-metashark/raw/main/doc/logo.png', "https://jellyfin-plugin-release.pages.dev/metashark/logo.png", manifest_cn)
    #     manifest_cn = re.sub('https://github.com/cxfksword/jellyfin-plugin-metashark/releases/download/v[0-9.]+', "https://jellyfin-plugin-release.pages.dev/metashark", manifest_cn)
    #     f.write(manifest_cn)


if __name__ == '__main__':
    main()