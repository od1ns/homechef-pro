import 'dart:io';
import 'dart:typed_data';

import 'package:flutter/foundation.dart';
import 'package:path_provider/path_provider.dart';
import 'package:share_plus/share_plus.dart';

/// Saves the bytes to a temp file and triggers the native share sheet
/// (mobile/desktop). On web we don't reach here — the admin_web has its own
/// `dart:html` blob download. For the customer app, share_plus on web also
/// works by triggering a download/share intent, so we still funnel through it.
Future<void> shareReceiptPdf({
  required List<int> bytes,
  required String filename,
}) async {
  if (kIsWeb) {
    // share_plus 10+ supports web via XFile.fromData
    await SharePlus.instance.share(
      ShareParams(
        files: [XFile.fromData(
          Uint8List.fromList(bytes),
          name: filename,
          mimeType: 'application/pdf',
        )],
        subject: filename,
      ),
    );
    return;
  }

  final dir = await getTemporaryDirectory();
  final file = File('${dir.path}/$filename');
  await file.writeAsBytes(bytes, flush: true);
  await SharePlus.instance.share(
    ShareParams(
      files: [XFile(file.path)],
      subject: filename,
    ),
  );
}
