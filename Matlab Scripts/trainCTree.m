function [ confusionMatrix, model, label ] = trainRUSBoost(trX, teX, trY,...
    teY, predictorNames, numlearners, otherArgs)
%TrainRUSBoost trains and tests an RUSBoost ensemble, returns confusion
%matrix, predictor, and the labels of the guesses on the test set.
%   Detailed explanation goes here
classifier = @fitctree;

numlearners =1;%Unused, there for compatibility with function signatures
rng(13, 'twister');

switch(length(otherArgs))
    case 2
        model = classifier(trX, trY, 'PredictorNames', predictorNames,...
            otherArgs{1}, otherArgs{2});
    case 4
        model = classifier(trX, trY, 'PredictorNames', predictorNames,...
            otherArgs{1}, otherArgs{2}, otherArgs{3}, otherArgs{4});
    case 6
        model = classifier(trX, trY,'PredictorNames', predictorNames,...
             otherArgs{1}, otherArgs{2}, otherArgs{3},...
            otherArgs{4}, otherArgs{5}, otherArgs{6});
    case 8
        model = classifier(trX, trY,'PredictorNames', predictorNames,...
             otherArgs{1}, otherArgs{2}, otherArgs{3},...
            otherArgs{4}, otherArgs{5}, otherArgs{6}, otherArgs{7}, otherArgs{8});
    case 10
        model = classifier(trX, trY,'PredictorNames', predictorNames,...
              otherArgs{1}, otherArgs{2}, otherArgs{3},...
            otherArgs{4}, otherArgs{5}, otherArgs{6}, otherArgs{7}, otherArgs{8}, ...
            otherArgs{9}, otherArgs{10});
    otherwise
        model = classifier(trX, trY, classifierType, numLearners, learners,...
            'PredictorNames', predictorNames, key1, val1 );
end

label = predict(model, teX);


confusionMatrix = confusionmat(teY, label);

	
% CVModel = crossval(model, 'Leaveout', 'on');
% CVLabels = kfoldPredict(CVModel);
end