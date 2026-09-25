from http.server import BaseHTTPRequestHandler, HTTPServer
from datetime import datetime, timezone
from pathlib import Path

DESTINATION = Path("/alerts/alerts.jsonl")
DESTINATION.parent.mkdir(parents=True, exist_ok=True)


class Handler(BaseHTTPRequestHandler):
    def do_POST(self):
        length = min(int(self.headers.get("Content-Length", "0")), 1_000_000)
        payload = self.rfile.read(length).decode("utf-8", "replace")
        with DESTINATION.open("a", encoding="utf-8") as stream:
            stream.write('{"receivedAt":"' + datetime.now(timezone.utc).isoformat()
                         + '","alert":' + payload + '}\n')
        self.send_response(200)
        self.end_headers()


HTTPServer(("0.0.0.0", 8080), Handler).serve_forever()
