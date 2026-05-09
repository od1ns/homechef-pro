import 'dart:io';

// ignore: unnecessary_import — el analyzer cree que es redundante porque
// share_plus re-exporta kIsWeb/Uint8List, pero al compilar real falla. Se
// queda explicito.
import 'package:flutter/foundation.dart';
import 'package:path_provider/path_provider.dart';
import 'package:share_plus/share_plus.dart';

/// Saves the bytes to a temp file and triggers the native share sheet
/// (mobile/desktop). En web `share_plus` redirige al share intent o al download
/// del blob — no necesitamos `dart:html` aca.
///
/// Compatible con share_plus 10.x (Share.shareXFiles).
Future<void> shareReceiptPdf({
  required List<int> bytes,
  required String filename,
}) async {
  if (kIsWeb) {
    final xfile = XFile.fromData(
      Uint8List.fromList(bytes),
      name: filename,
      mimeType: 'application/pdf',
    );
    await Share.shareXFiles([xfile], subject: filename);
    return;
  }

  final dir = await getTemporaryDirectory();
  final file = File('${dir.path}/$filename');
  await file.writeAsBytes(bytes, flush: true);
  await Share.shareXFiles(
    [XFile(file.path, mimeType: 'application/pdf')],
    subject: filename,
  );
}
