// Web-only implementation. The conditional import in download_helper.dart
// only loads this when dart.library.html is available.
// ignore: deprecated_member_use, avoid_web_libraries_in_flutter
import 'dart:html' as html;
import 'dart:typed_data';

void downloadBytes(List<int> bytes, String filename, String contentType) {
  final blob = html.Blob([Uint8List.fromList(bytes)], contentType);
  final url = html.Url.createObjectUrlFromBlob(blob);
  final anchor = html.AnchorElement(href: url)
    ..setAttribute('download', filename)
    ..style.display = 'none';
  html.document.body?.append(anchor);
  anchor.click();
  anchor.remove();
  html.Url.revokeObjectUrl(url);
}
