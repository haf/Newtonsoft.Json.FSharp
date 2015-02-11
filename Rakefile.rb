require 'bundler/setup'

require 'rake/clean'
require 'albacore'
require 'albacore/tasks/versionizer'
require 'albacore/tasks/release'

require 'albacore/ext/teamcity'

Albacore::Tasks::Versionizer.new :versioning
Configuration = ENV['CONFIGURATION'] || 'Release'

build :build_clean do |b|
  b.target = 'Clean'
  b.prop 'Configuration', Configuration
  b.sln = 'src/Newtonsoft.Json.FSharp.sln'
end

task :clean => :build_clean

desc 'Perform fast compilation (warn: doesn\'t d/l deps)'
build :quick_compile do |b|
  b.prop 'Configuration', Configuration
  b.sln = 'src/Newtonsoft.Json.FSharp.sln'
end

task :paket_bootstrap do
    system 'tools/paket.bootstrapper.exe', clr_command: true unless \
          File.exists? 'tools/paket.exe'
end

desc 'restore all nugets as per the packages.config files'
task :restore => :paket_bootstrap do
    system 'tools/paket.exe', 'restore', clr_command: true
end

desc 'create assembly infos'
asmver_files :assembly_info => :versioning do |a|
  a.files = FileList['**/*proj'] # optional, will find all projects recursively by default

  # attributes are required:
  a.attributes assembly_description: 'Different serializers for Newtonsoft.Json, making it easier to work with JSON data with Newtonsoft.Json from F#.',
               assembly_configuration: Configuration,
               assembly_company: 'Logibit AB',
               assembly_copyright: "(c) #{Time.now.year} by Henrik Feldt",
               assembly_version: ENV['LONG_VERSION'],
               assembly_file_version: ENV['LONG_VERSION'],
               assembly_informational_version: ENV['BUILD_VERSION']
end

desc 'perform full compilation'
build :compile => [:versioning, :assembly_info, :restore] do |b|
  b.prop 'Configuration', Configuration
  b.sln = 'src/Newtonsoft.Json.FSharp.sln'
end

task :tests_quick do
  system "src/JsonNet.Tests/bin/#{Configuration}/Newtonsoft.Json.FSharp.Tests.exe", clr_command: true
end

desc 'run the unit tests'
task :tests => [:tests_quick, :compile]

CLEAN.add 'build'
directory 'build/pkg'

desc 'package nugets - finds all projects and package them'
nugets_pack :create_nugets => ['build/pkg', :versioning, :compile, :tests] do |p|
  p.configuration = Configuration
  p.files   = FileList['src/**/*.{csproj,fsproj,nuspec}'].
    exclude(/Tests/)
  p.out     = 'build/pkg'
  p.exe     = 'tools/NuGet.exe'
  p.with_metadata do |m|
    m.description = 'Different serializers for Newtonsoft.Json, making it easier to work with JSON data with Newtonsoft.Json from F#.'
    m.authors = 'Henrik Feldt, Logibit AB'
    m.version = ENV['NUGET_VERSION']
  end
end

task :ensure_key do
  raise 'missing env NUGET_KEY' unless ENV['NUGET_KEY']
end

Albacore::Tasks::Release.new :release,
                             pkg_dir: 'build/pkg',
                             depend_on: [:create_nugets, :ensure_key],
                             nuget_exe: 'tools/NuGet.exe',
                             api_key: ENV['NUGET_KEY']

task :default => :create_nugets
