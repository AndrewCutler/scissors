#!/usr/bin/env bash
set -euo pipefail

Xvfb :99 -screen 0 1280x720x24 -ac +extension RANDR +render -noreset >/tmp/xvfb.log 2>&1 &
xvfb_pid=$!

cleanup() {
    kill "${xvfb_pid}" >/dev/null 2>&1 || true
}

trap cleanup EXIT

until xdpyinfo -display :99 >/dev/null 2>&1; do
    sleep 0.2
done

fluxbox >/tmp/fluxbox.log 2>&1 &
x11vnc -display :99 -rfbport 5900 -forever -shared -nopw -xkb >/tmp/x11vnc.log 2>&1 &

exec dotnet Scissors.Desktop.dll
