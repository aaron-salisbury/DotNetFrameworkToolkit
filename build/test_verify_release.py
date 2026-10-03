import copy
import json
from pathlib import Path
import tempfile
import unittest
import zipfile

from verify_release import PACKAGE_ID, hash_file, verify


class ReleaseHandoffTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.directory = Path(self.temporary.name)
        self.commit = 'a' * 40
        self.version = '0.3.0-rc.1'
        self.report = dict(PackageVersion=self.version, SourceCommit=self.commit,
                           Configuration='Release', Framework='net20', RuntimeMetadata='v2.0.50727',
                           CorruptPackageFixturesRejected=7, ImagesAndOverwriteVerified=True)
        for extension, field in [('nupkg', 'PackageSha256'), ('snupkg', 'SymbolsSha256')]:
            path = self.directory / (PACKAGE_ID + '.' + self.version + '.' + extension)
            with zipfile.ZipFile(path, 'w') as archive:
                archive.writestr(PACKAGE_ID + '.nuspec',
                                 '<package><metadata><id>' + PACKAGE_ID + '</id><version>' + self.version
                                 + '</version><repository commit="' + self.commit + '" /></metadata></package>')
            self.report[field] = hash_file(path)
        self.consumer = dict(PackageVersion=self.version, PackageSha256=self.report['PackageSha256'],
                             Consumers=[dict(Architecture=architecture, Runtime='CLR 2.0', Passed=True)
                                        for architecture in ['x86', 'x64']])
        (self.directory / 'release-notes.md').write_text('Release notes', encoding='utf-8')
        self.write_reports()

    def write_reports(self):
        (self.directory / 'validation.json').write_text(json.dumps(self.report), encoding='utf-8')
        (self.directory / 'consumer-validation.json').write_text(json.dumps(self.consumer), encoding='utf-8')

    def check(self, tag=None, commit=None):
        return verify(self.directory, tag or 'v' + self.version, commit or self.commit)

    def test_valid_prerelease(self):
        self.assertEqual(5, len(self.check()))

    def test_wrong_tag_and_source_commit(self):
        for kwargs in [dict(tag='v0.3.0'), dict(commit='b' * 40)]:
            with self.subTest(kwargs=kwargs), self.assertRaises(ValueError):
                self.check(**kwargs)

    def test_each_package_hash_is_checked(self):
        for extension in ['nupkg', 'snupkg']:
            with self.subTest(extension=extension):
                path = self.directory / (PACKAGE_ID + '.' + self.version + '.' + extension)
                original = path.read_bytes()
                path.write_bytes(original + b'changed')
                with self.assertRaises(ValueError):
                    self.check()
                path.write_bytes(original)

    def test_failed_or_missing_consumer_and_wrong_package(self):
        original = copy.deepcopy(self.consumer)
        for mutation in ['failure', 'missing', 'hash']:
            self.consumer = copy.deepcopy(original)
            if mutation == 'failure':
                self.consumer['Consumers'][0]['Passed'] = False
            elif mutation == 'missing':
                self.consumer['Consumers'].pop()
            else:
                self.consumer['PackageSha256'] = '0' * 64
            self.write_reports()
            with self.subTest(mutation=mutation), self.assertRaises(ValueError):
                self.check()

    def test_unverified_build_is_rejected(self):
        original = self.report.copy()
        for field, value in [('Configuration', 'Debug'), ('Framework', 'net481'),
                             ('ImagesAndOverwriteVerified', False), ('CorruptPackageFixturesRejected', 0)]:
            self.report = original.copy()
            self.report[field] = value
            self.write_reports()
            with self.subTest(field=field), self.assertRaises(ValueError):
                self.check()

    def test_extra_files_and_empty_notes_are_rejected(self):
        extra = self.directory / 'unexpected.nupkg'
        extra.write_bytes(b'extra')
        with self.assertRaises(ValueError):
            self.check()
        extra.unlink()
        (self.directory / 'release-notes.md').write_text('   ', encoding='utf-8')
        with self.assertRaises(ValueError):
            self.check()


if __name__ == '__main__':
    unittest.main()
