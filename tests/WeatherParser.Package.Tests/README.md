# Paketkonsumententests – P1.04

[Deutsch](README.md) | [English](README.en.md)

Dieser Bereich ist für Tests des tatsächlich gebauten NuGet-Pakets reserviert. Das Testprojekt wird in P1.04 angelegt, sobald lokales Pack und isolierter Paket-Restore zusammen eingerichtet werden. Es gehört noch nicht zur Solution.

Der spätere Konsument verwendet eine versionierte `PackageReference` auf das lokale `.nupkg`, keine `ProjectReference` auf den Parserquellcode. Paketversion, isolierte Quelle und PDB-/Quellzuordnung werden geprüft. Hier entsteht kein CLI-Nutzungsbeispiel; dieses folgt erst nach der fertigen Bibliothek in P3.
