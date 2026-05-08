// Cross-platform download dispatcher: web triggers a real `<a download>`,
// non-web is a no-op (admin web is the only target right now).
export 'download_stub.dart' if (dart.library.html) 'download_web.dart';
