function [ cvLabels, label ] = longMCSave( doSave,path, funcstr, numlearners, trX, teX, trY,...
    teY, predictorNames, otherArgs)
	func = str2func(funcstr)
	[confumat, model, label] =  func(trX, teX, trY, teY, predictorNames, numlearners, otherArgs);
	cvLabels = getCVLabels(model);
	if(doSave==1)
		saveModel(path, model);
	end
	