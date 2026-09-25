Pod::Spec.new do |spec|
  spec.name         = 'ChillSharpObjectiveCClient'
  spec.version      = '0.1.0'
  spec.summary      = 'Generic Objective-C client for ChillSharp HTTP APIs.'
  spec.description  = 'Foundation-based asynchronous Objective-C client for generic ChillSharp services.'
  spec.homepage     = 'https://github.com/e-500/chill-sharp'
  spec.license      = { :type => 'AGPL-3.0-or-later' }
  spec.author       = 'Andrea Piovesan'
  spec.source       = { :git => 'https://github.com/e-500/chill-sharp.git', :tag => spec.version.to_s }
  spec.platform     = :ios, '13.0'
  spec.source_files = 'Sources/**/*.{h,m}'
  spec.public_header_files = 'Sources/**/*.h'
  spec.frameworks   = 'Foundation'
  spec.requires_arc = true
end
