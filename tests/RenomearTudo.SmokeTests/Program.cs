using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using RenomearTudo.Core.Models;
using RenomearTudo.Core.Services;

namespace RenomearTudo.SmokeTests
{
    internal static class Program
    {
        private static int Main()
        {
            var root = Path.Combine(Path.GetTempPath(), "RenomearTudo-Smoke-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                File.WriteAllText(Path.Combine(root, "A.txt"), "a");
                File.WriteAllText(Path.Combine(root, "B.txt"), "b");

                TestPreview(root);
                TestSimpleReplaceIgnoreCase(root);
                TestSwapRenameAndUndo(root);
                TestUndoDestinationDirectoryConflict(root);
                TestCaseOnlyRename(root);
                TestDuplicateConflict(root);
                TestExistingDestinationConflict(root);
                TestDirectoryDestinationConflict(root);
                TestConflictChainStabilization(root);
                TestInvalidName(root);
                TestInvalidCharactersDoNotThrow(root);
                TestTooLongFileName(root);
                TestAccentRemoval();

                Console.WriteLine("Smoke tests: OK");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Smoke tests: FALHOU\n" + ex);
                return 1;
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        }

        private static void TestPreview(string root)
        {
            var item = new FileRenameItem(Path.Combine(root, "A.txt"));
            var rules = new List<RenameRule>
            {
                new RenameRule { Type = RenameRuleType.Prefix, Text1 = "Foto_" },
                new RenameRule { Type = RenameRuleType.Numbering, Text1 = "{nome}_{numero}", NumberStart = 1, NumberPadding = 3 }
            };
            var name = RenameEngine.BuildPreviewName(item, rules, 0, 1);
            Assert(name == "Foto_A_001.txt", "Preview inesperada: " + name);
        }

        private static void TestSimpleReplaceIgnoreCase(string root)
        {
            var item = new FileRenameItem(Path.Combine(root, "A.txt"));
            var rules = new List<RenameRule>
            {
                new RenameRule { Type = RenameRuleType.Replace, Text1 = "a", Text2 = "$literal", IgnoreCase = true }
            };
            var name = RenameEngine.BuildPreviewName(item, rules, 0, 1);
            Assert(name == "$literal.txt", "Substituição literal case-insensitive incorreta: " + name);
        }

        private static void TestSwapRenameAndUndo(string root)
        {
            var a = new FileRenameItem(Path.Combine(root, "A.txt")) { PreviewName = "B.txt" };
            var b = new FileRenameItem(Path.Combine(root, "B.txt")) { PreviewName = "A.txt" };
            var items = new List<FileRenameItem> { a, b };
            RenameEngine.ValidatePreview(items);
            Assert(items.All(i => i.Status == RenameItemStatus.Ready), "Troca A/B deveria ser válida");

            var result = RenameEngine.Execute(items, CancellationToken.None);
            Assert(result.Success && result.Records.Count == 2, "Renomeação em duas fases falhou");
            Assert(File.ReadAllText(Path.Combine(root, "A.txt")) == "b", "Conteúdo A incorreto após troca");
            Assert(File.ReadAllText(Path.Combine(root, "B.txt")) == "a", "Conteúdo B incorreto após troca");

            var undo = RenameEngine.Undo(result.Records);
            Assert(undo.Success, "Undo falhou");
            Assert(File.ReadAllText(Path.Combine(root, "A.txt")) == "a", "Conteúdo A incorreto após undo");
            Assert(File.ReadAllText(Path.Combine(root, "B.txt")) == "b", "Conteúdo B incorreto após undo");
        }

        private static void TestUndoDestinationDirectoryConflict(string root)
        {
            var source = Path.Combine(root, "UndoSource.txt");
            var target = Path.Combine(root, "UndoTarget.txt");
            File.WriteAllText(source, "undo");

            var item = new FileRenameItem(source) { PreviewName = "UndoTarget.txt" };
            RenameEngine.ValidatePreview(new List<FileRenameItem> { item });
            var rename = RenameEngine.Execute(new List<FileRenameItem> { item }, CancellationToken.None);
            Assert(rename.Success && File.Exists(target), "Preparação do teste de Undo falhou");

            Directory.CreateDirectory(source);
            var blockedUndo = RenameEngine.Undo(rename.Records);
            Assert(!blockedUndo.Success, "Undo deveria detectar pasta ocupando o destino original");
            Assert(File.Exists(target), "Undo bloqueado não deveria mover o arquivo");

            Directory.Delete(source);
            var undo = RenameEngine.Undo(rename.Records);
            Assert(undo.Success && File.Exists(source), "Undo deveria funcionar após remover o conflito");
            File.Delete(source);
        }

        private static void TestCaseOnlyRename(string root)
        {
            var path = Path.Combine(root, "case-name.txt");
            File.WriteAllText(path, "case");
            var item = new FileRenameItem(path) { PreviewName = "CASE-NAME.txt" };
            RenameEngine.ValidatePreview(new List<FileRenameItem> { item });
            Assert(item.Status == RenameItemStatus.Ready, "Mudança apenas de caixa deveria ser válida");
            var result = RenameEngine.Execute(new List<FileRenameItem> { item }, CancellationToken.None);
            Assert(result.Success && File.Exists(Path.Combine(root, "CASE-NAME.txt")), "Mudança apenas de caixa falhou");
        }

        private static void TestDuplicateConflict(string root)
        {
            var a = new FileRenameItem(Path.Combine(root, "A.txt")) { PreviewName = "Mesmo.txt" };
            var b = new FileRenameItem(Path.Combine(root, "B.txt")) { PreviewName = "Mesmo.txt" };
            RenameEngine.ValidatePreview(new List<FileRenameItem> { a, b });
            Assert(a.Status == RenameItemStatus.Conflict && b.Status == RenameItemStatus.Conflict, "Destino duplicado deveria gerar conflito");
        }


        private static void TestExistingDestinationConflict(string root)
        {
            var existing = Path.Combine(root, "DestinoExistente.txt");
            File.WriteAllText(existing, "destino");
            var item = new FileRenameItem(Path.Combine(root, "A.txt")) { PreviewName = "DestinoExistente.txt" };
            RenameEngine.ValidatePreview(new List<FileRenameItem> { item });
            Assert(item.Status == RenameItemStatus.Conflict, "Arquivo de destino já existente deveria gerar conflito");
            File.Delete(existing);
        }

        private static void TestDirectoryDestinationConflict(string root)
        {
            var directory = Path.Combine(root, "DestinoPasta");
            Directory.CreateDirectory(directory);
            var item = new FileRenameItem(Path.Combine(root, "A.txt")) { PreviewName = "DestinoPasta" };
            RenameEngine.ValidatePreview(new List<FileRenameItem> { item });
            Assert(item.Status == RenameItemStatus.Conflict, "Pasta de destino já existente deveria gerar conflito");
            Directory.Delete(directory);
        }

        private static void TestConflictChainStabilization(string root)
        {
            var cPath = Path.Combine(root, "C.txt");
            File.WriteAllText(cPath, "c");
            var a = new FileRenameItem(Path.Combine(root, "A.txt")) { PreviewName = "B.txt" };
            var b = new FileRenameItem(Path.Combine(root, "B.txt")) { PreviewName = "C.txt" };
            RenameEngine.ValidatePreview(new List<FileRenameItem> { a, b });
            Assert(b.Status == RenameItemStatus.Conflict, "B→C deveria conflitar com C existente");
            Assert(a.Status == RenameItemStatus.Conflict, "A→B também deve conflitar quando B deixa de ser uma origem móvel");
            File.Delete(cPath);
        }

        private static void TestInvalidCharactersDoNotThrow(string root)
        {
            var item = new FileRenameItem(Path.Combine(root, "A.txt")) { PreviewName = "nome?.txt" };
            RenameEngine.ValidatePreview(new List<FileRenameItem> { item });
            Assert(item.Status == RenameItemStatus.Invalid, "Caracteres inválidos deveriam ser rejeitados sem lançar exceção");
        }

        private static void TestTooLongFileName(string root)
        {
            var item = new FileRenameItem(Path.Combine(root, "A.txt")) { PreviewName = new string('a', 256) };
            RenameEngine.ValidatePreview(new List<FileRenameItem> { item });
            Assert(item.Status == RenameItemStatus.Invalid, "Nome com mais de 255 caracteres deveria ser inválido");
        }

        private static void TestInvalidName(string root)
        {
            var item = new FileRenameItem(Path.Combine(root, "A.txt")) { PreviewName = "CON.txt" };
            RenameEngine.ValidatePreview(new List<FileRenameItem> { item });
            Assert(item.Status == RenameItemStatus.Invalid, "Nome reservado deveria ser inválido");
        }

        private static void TestAccentRemoval()
        {
            var value = FileNameSanitizer.RemoveAccents("Música João Açúcar");
            Assert(value == "Musica Joao Acucar", "Remoção de acentos incorreta: " + value);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
