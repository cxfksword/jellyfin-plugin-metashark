#!/bin/bash

# $1 from github action
ARTIFACT=$1
VERSION=$2
TAG=$3

CURRENT_DATE=$(date +'%Y-%m-%dT%H:%M:%S')
WORK_DIR=$(cd -P -- "$(dirname -- "$0")" && pwd -P)
ARTIFACT_ZIP_FILE="${WORK_DIR}/artifacts/artifacts.zip"
ARTIFACT_META="${WORK_DIR}/build.meta.json"

JELLYFIN_REPO_URL="https://github.com/cxfksword/jellyfin-plugin-metashark/releases/download"
JELLYFIN_MANIFEST="${WORK_DIR}/manifest.json"
JELLYFIN_MANIFEST_CN="${WORK_DIR}/manifest_cn.json"
JELLYFIN_MANIFEST_OLD="https://github.com/cxfksword/jellyfin-plugin-metashark/releases/download/manifest/manifest.json"

# download old manifest
wget -q -O "$JELLYFIN_MANIFEST" "$JELLYFIN_MANIFEST_OLD"
if [ $? -ne 0 ]; then
    rm -rf $JELLYFIN_MANIFEST
    jprm repo init $WORK_DIR
fi

# update meta json message
cp -f "${ARTIFACT_META}" "${ARTIFACT_ZIP_FILE}.meta.json"
CHANGELOG=$(git tag -l --format='%(contents)' ${TAG})
sed -i "s@NA@$CHANGELOG@" "${ARTIFACT_ZIP_FILE}.meta.json"
sed -i "s@1.0.0.0@$VERSION@" "${ARTIFACT_ZIP_FILE}.meta.json"
sed -i "s@1970-01-01T00:00:00Z@$CURRENT_DATE@" "${ARTIFACT_ZIP_FILE}.meta.json"


# generate new manifest
jprm --verbosity=debug repo add --url=${JELLYFIN_REPO_URL} "${JELLYFIN_MANIFEST}" "${ARTIFACT_ZIP_FILE}"

# fix menifest download url
sed -i "s@/${ARTIFACT}/@/$TAG/@" "$JELLYFIN_MANIFEST"

# 国内加速
cp -f "$JELLYFIN_MANIFEST" "$JELLYFIN_MANIFEST_CN"
sed -i "s@github.com@ghproxy.com/https://github.com@g" "$JELLYFIN_MANIFEST_CN"

exit $?