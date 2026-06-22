#!/bin/bash
cd "$(dirname "$0")"
BUILD_DIR="Builds/WebGL_Local"
PORT=8000
if [ ! -f "$BUILD_DIR/index.html" ]; then
  echo "index.html が見つかりません。先に WebGL ビルドしてください。"
  echo "探した場所: $BUILD_DIR/index.html"
  exit 1
fi
echo "WebGL local server starting..."
echo "Mac local:"
echo "  http://localhost:$PORT/"
echo ""
IP=$(ipconfig getifaddr en0 2>/dev/null)
if [ -z "$IP" ]; then
  IP=$(ifconfig | awk '/^[a-z0-9]+:/{iface=$1; sub(":","",iface)} /inet / && $2 !~ /^127\./ {print $2; exit}')
fi
if [ -n "$IP" ]; then
  echo "iPhone / same Wi-Fi:"
  echo "  http://$IP:$PORT/"
else
  echo "MacのWi-Fi IPを取得できませんでした。"
  echo "System Settings > Wi-Fi からIPを確認してください。"
fi
echo ""
echo "Press Ctrl+C to stop."
python3 -m http.server "$PORT" --directory "$BUILD_DIR" --bind 0.0.0.0
