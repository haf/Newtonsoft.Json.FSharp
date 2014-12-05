require 'bundler/setup'

require 'albacore'
require 'albacore/tasks/versionizer'
require 'albacore/tasks/release'

require 'albacore/ext/teamcity'

Albacore::Tasks::Versionizer.new :versioning
Configuration = 'Release'

desc 'Perform fast compilation (warn: doesn\'t d/l deps)'
build :quick_compile do |b|
  b.sln = 'src/Intelliplan.JsonNet.sln'
end

desc 'restore all nugets as per the packages.config files'
nugets_restore :restore do |p|
  p.out = 'src/packages'
  p.exe = 'buildsupport/NuGet.exe'
end

desc 'create assembly infos'
asmver_files :assembly_info => :versioning do |a|
  a.files = FileList['**/*proj'] # optional, will find all projects recursively by default

  # attributes are required:
  a.attributes assembly_description: 'Different serializers for Newtonsoft.Json, making it easier to work with JSON data with Newtonsoft.Json from F#.',
               assembly_configuration: Configuration,
               assembly_company: 'Intelliplan International AB, Logibit AB',
               assembly_copyright: "(c) #{Time.now.year} by Henrik Feldt",
               assembly_version: ENV['LONG_VERSION'],
               assembly_file_version: ENV['LONG_VERSION'],
               assembly_informational_version: ENV['BUILD_VERSION']
end

desc 'perform full compilation'
build :compile => [:versioning, :assembly_info, :restore] do |b|
  b.prop 'Configuration', Configuration
  b.sln = 'src/Intelliplan.JsonNet.sln'
end

task :tests_quick do
  system "src/JsonNet.Tests/bin/#{Configuration}/Intelliplan.JsonNet.Tests.exe", clr_command: true
end

desc 'run the unit tests'
task :tests => [:tests_quick, :compile]

directory 'build/pkg'

desc 'package nugets - finds all projects and package them'
nugets_pack :create_nugets => ['build/pkg', :versioning, :compile, :tests] do |p|
  p.files   = FileList['src/**/*.{csproj,fsproj,nuspec}'].
    exclude(/Tests/)
  p.out     = 'build/pkg'
  p.exe     = 'buildsupport/NuGet.exe'
  p.with_metadata do |m|
    m.description = 'Different serializers for Newtonsoft.Json, making it easier to work with JSON data with Newtonsoft.Json from F#.'
    m.authors = 'Henrik Feldt, Logibit AB'
    m.version = ENV['NUGET_VERSION']
  end
end

Albacore::Tasks::Release.new :release,
                             pkg_dir: 'build/pkg',
                             depend_on: :create_nugets,
                             nuget_exe: 'buildsupport/NuGet.exe',
                             api_key: ENV['NUGET_KEY']

task :default => :create_nugets
